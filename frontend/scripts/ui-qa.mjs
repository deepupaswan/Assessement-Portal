import fs from 'node:fs/promises';
import path from 'node:path';
import { chromium, request } from '@playwright/test';

const FRONTEND_URL = 'http://localhost:4200';
const API_URL = 'http://localhost:7080';
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const ARTIFACT_DIR = path.resolve('playwright-artifacts', 'ui-qa');
const PASSWORD = 'QaPass!2345';

const checks = [];
const findings = [];

function logCheck(status, name, details = '') {
  checks.push({ status, name, details });
  const suffix = details ? ` - ${details}` : '';
  console.log(`[${status}] ${name}${suffix}`);
}

function addFinding(severity, title, details) {
  findings.push({ severity, title, details });
  console.log(`[FINDING:${severity}] ${title} - ${details}`);
}

async function ensureDir(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

async function screenshot(page, fileName) {
  await ensureDir(ARTIFACT_DIR);
  const fullPath = path.join(ARTIFACT_DIR, fileName);
  await page.screenshot({ path: fullPath, fullPage: true });
  return fullPath;
}

async function getStoredUser(page) {
  return page.evaluate(() => {
    const raw = window.localStorage.getItem('portal.auth.user');
    return raw ? JSON.parse(raw) : null;
  });
}

async function registerUser(page, { name, email, role }) {
  await page.goto(`${FRONTEND_URL}/auth/register`, { waitUntil: 'domcontentloaded' });
  await page.getByLabel('Name').fill(name);
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(PASSWORD);
  await page.getByLabel(role).check();
  await page.getByRole('button', { name: 'Create account' }).click();

  const expectedUrlPart = role === 'Admin' ? '/admin' : '/candidate';
  const expectedHeading = role === 'Admin' ? 'Dashboard' : 'Candidate Workspace';
  await page.waitForURL(`**${expectedUrlPart}**`, { timeout: 20000 });
  await page.getByRole('heading', { name: expectedHeading }).waitFor({ timeout: 20000 });
}

async function loginUser(page, { email, role }) {
  await page.goto(`${FRONTEND_URL}/auth/login`, { waitUntil: 'domcontentloaded' });
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(PASSWORD);
  await page.getByRole('button', { name: 'Sign in' }).click();

  const expectedUrlPart = role === 'Admin' ? '/admin' : '/candidate';
  const expectedHeading = role === 'Admin' ? 'Dashboard' : 'Candidate Workspace';
  await page.waitForURL(`**${expectedUrlPart}**`, { timeout: 20000 });
  await page.getByRole('heading', { name: expectedHeading }).waitFor({ timeout: 20000 });
}

async function createApiContext(token) {
  return request.newContext({
    baseURL: API_URL,
    extraHTTPHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

async function createAssessmentViaApi(api, title) {
  const response = await api.post('/api/assessments', {
    data: {
      title,
      description: 'Browser QA assessment',
      durationMinutes: 30,
      randomizeQuestions: false
    }
  });

  if (!response.ok()) {
    throw new Error(`Assessment create failed: ${response.status()} ${await response.text()}`);
  }

  return response.json();
}

async function createQuestionViaApi(api, assessmentId) {
  const response = await api.post(`/api/assessments/${assessmentId}/questions`, {
    data: {
      text: 'Which option is marked correct?',
      type: 'MCQ',
      maxScore: 1,
      correctAnswer: 'Correct option',
      isRequired: true,
      order: 1,
      options: [
        { text: 'Correct option', isCorrect: true, order: 1 },
        { text: 'Wrong option', isCorrect: false, order: 2 }
      ]
    }
  });

  if (!response.ok()) {
    throw new Error(`Question create failed: ${response.status()} ${await response.text()}`);
  }

  return response.json();
}

async function listCandidatesViaApi(api) {
  const response = await api.get('/api/candidates');
  if (!response.ok()) {
    throw new Error(`Candidate list failed: ${response.status()} ${await response.text()}`);
  }

  return response.json();
}

async function assignAssessmentViaApi(api, candidateId, assessmentId) {
  const response = await api.post('/api/candidates/assignments', {
    data: {
      candidateId,
      assessmentId
    }
  });

  if (!response.ok()) {
    throw new Error(`Assignment create failed: ${response.status()} ${await response.text()}`);
  }

  return response.json();
}

async function waitForVisible(locator, timeout = 15000) {
  await locator.waitFor({ state: 'visible', timeout });
}

async function run() {
  const browser = await chromium.launch({
    headless: true,
    executablePath: CHROME_PATH
  });

  const runId = Date.now();
  const admin = {
    name: 'Admin Qa',
    email: `admin.ui.${runId}@example.com`
  };
  const candidate = {
    name: 'Candidate Qa',
    email: `candidate.ui.${runId}@example.com`
  };
  let adminPage;
  let adminContext;
  let candidatePage;
  let api;

  try {
    adminContext = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
    adminPage = await adminContext.newPage();
    adminPage.on('dialog', async dialog => dialog.accept());

    await registerUser(adminPage, { ...admin, role: 'Admin' });
    logCheck('PASS', 'Admin registration and redirect', admin.email);

    const storedAdmin = await getStoredUser(adminPage);
    if (!storedAdmin?.token) {
      throw new Error('Admin token was not persisted to local storage.');
    }

    api = await createApiContext(storedAdmin.token);

    candidatePage = await (await browser.newContext({ viewport: { width: 1440, height: 1100 } })).newPage();
    candidatePage.on('dialog', async dialog => dialog.accept());
    await registerUser(candidatePage, { ...candidate, role: 'Candidate' });
    logCheck('PASS', 'Candidate registration and redirect', candidate.email);
    await candidatePage.close();

    await adminPage.goto(`${FRONTEND_URL}/admin/candidates`, { waitUntil: 'domcontentloaded' });
    await waitForVisible(adminPage.getByRole('heading', { name: 'Candidates' }));
    logCheck('PASS', 'Admin candidates page loads');

    await adminPage.getByRole('button', { name: /Add Candidate/i }).click();
    await adminPage.locator('#name').fill(candidate.name);
    await adminPage.locator('#email').fill(candidate.email);
    await adminPage.getByRole('button', { name: /^Create$/ }).click();
    await waitForVisible(adminPage.locator('table').getByText(candidate.email), 20000);
    logCheck('PASS', 'Candidate record can be created from admin UI');

    const candidates = await listCandidatesViaApi(api);
    const createdCandidate = candidates.find(item => item.email?.toLowerCase() === candidate.email.toLowerCase());
    if (!createdCandidate?.id) {
      throw new Error('Created candidate was not returned by candidate API.');
    }

    await adminPage.goto(`${FRONTEND_URL}/admin/assessments`, { waitUntil: 'domcontentloaded' });
    await waitForVisible(adminPage.getByRole('heading', { name: 'Assessments' }));
    logCheck('PASS', 'Admin assessments page loads');

    await adminPage.getByRole('button', { name: /Create Assessment/i }).click();
    await adminPage.waitForTimeout(1500);
    const createAssessmentUrl = adminPage.url();
    const contentLocator = adminPage.locator('main.admin-content');
    const hasMainContent = await contentLocator.isVisible().catch(() => false);
    const createAssessmentBody = hasMainContent
      ? ((await contentLocator.textContent()) ?? '').trim()
      : '';
    const createRouteBroken =
      createAssessmentUrl.endsWith('/admin/assessments/create') &&
      (!hasMainContent || !/title|description|duration|assessment/i.test(createAssessmentBody));

    if (createRouteBroken) {
      addFinding(
        'high',
        'Create Assessment UI route is broken',
        'Clicking "Create Assessment" navigates to /admin/assessments/create but renders blank content, so the admin cannot create an assessment from the browser.'
      );
      await screenshot(adminPage, 'create-assessment-route-broken.png');
    } else {
      logCheck('PASS', 'Create Assessment button renders a page');
    }

    const assessmentTitle = `QA Browser Assessment ${runId}`;
    const createdAssessment = await createAssessmentViaApi(api, assessmentTitle);
    await createQuestionViaApi(api, createdAssessment.id);
    logCheck('PASS', 'Assessment and question setup completed through API workaround', assessmentTitle);

    await adminPage.goto(`${FRONTEND_URL}/admin/assignments`, { waitUntil: 'domcontentloaded' });
    await waitForVisible(adminPage.getByRole('heading', { name: 'Assignments' }));
    logCheck('PASS', 'Admin assignments page loads');

    await adminPage.getByRole('button', { name: /New Assignment/i }).click();
    await adminPage.locator('#candidate').selectOption(createdCandidate.id);
    const assessmentOptions = await adminPage.locator('#assessment option').evaluateAll(options =>
      options.map(option => ({ value: option.value, text: option.textContent?.trim() ?? '' }))
    );
    const assessmentOptionExists = assessmentOptions.some(option => option.value === createdAssessment.id);

    if (!assessmentOptionExists) {
      addFinding(
        'high',
        'Assignment UI does not expose assessments to assign',
        'The admin assignment form loads, but the assessment dropdown omits the newly created assessment, so assignments cannot be completed from the browser UI.'
      );
      await screenshot(adminPage, 'assignment-dropdown-missing-assessment.png');
      await assignAssessmentViaApi(api, createdCandidate.id, createdAssessment.id);
      await adminPage.goto(`${FRONTEND_URL}/admin/assignments`, { waitUntil: 'domcontentloaded' });
      await waitForVisible(adminPage.locator('table').getByText(assessmentTitle), 20000);
      logCheck('PASS', 'Assignment appears after API fallback setup');
    } else {
      await adminPage.locator('#assessment').selectOption(createdAssessment.id);
      await adminPage.getByRole('button', { name: /^Create$/ }).click();
      await waitForVisible(adminPage.locator('table').getByText(assessmentTitle), 20000);
      logCheck('PASS', 'Assignment can be created from admin UI');
    }

    await adminPage.goto(`${FRONTEND_URL}/admin/monitoring`, { waitUntil: 'domcontentloaded' });
    await waitForVisible(adminPage.getByRole('heading', { name: 'Live Monitoring' }));
    logCheck('PASS', 'Admin monitoring page loads');

    const candidateContext = await browser.newContext({ viewport: { width: 1440, height: 1100 } });
    const candidateLoginPage = await candidateContext.newPage();
    candidateLoginPage.on('dialog', async dialog => dialog.accept());
    await loginUser(candidateLoginPage, { email: candidate.email, role: 'Candidate' });
    logCheck('PASS', 'Candidate login succeeds in browser');

    const assignmentRow = candidateLoginPage.locator('tr', { hasText: assessmentTitle }).first();
    await waitForVisible(assignmentRow, 20000);
    await assignmentRow.getByRole('button').click();
    await candidateLoginPage.waitForURL('**/candidate/assessment/**', { timeout: 20000 });
    await waitForVisible(candidateLoginPage.getByText('Which option is marked correct?'));
    logCheck('PASS', 'Candidate can open assigned assessment');

    await candidateLoginPage.getByLabel('Correct option').check();
    await candidateLoginPage.getByRole('button', { name: /Submit Assessment/i }).click();
    await candidateLoginPage.waitForURL('**/candidate/result/**', { timeout: 30000 });
    await waitForVisible(candidateLoginPage.getByRole('heading', { name: 'Result Summary' }), 20000);
    await waitForVisible(candidateLoginPage.getByText('1 / 1'), 20000);
    logCheck('PASS', 'Candidate can submit assessment and see result summary');

    await adminPage.goto(`${FRONTEND_URL}/admin/analytics`, { waitUntil: 'domcontentloaded' });
    await waitForVisible(adminPage.getByRole('heading', { name: 'Analytics' }));
    await waitForVisible(adminPage.getByText(candidate.email), 20000);
    logCheck('PASS', 'Admin analytics page reflects submitted result');

    await adminPage.locator('.sidebar-footer .btn-logout').click();
    await adminPage.waitForTimeout(1500);
    const storedAfterLogout = await getStoredUser(adminPage);
    if (storedAfterLogout?.token) {
      addFinding(
        'medium',
        'Logout button does not log the user out',
        'Clicking the admin sidebar logout button leaves portal.auth.user in localStorage and does not navigate away from the protected area.'
      );
      await screenshot(adminPage, 'logout-button-noop.png');
    } else {
      logCheck('PASS', 'Logout clears the current session');
    }

    const report = {
      executedAtUtc: new Date().toISOString(),
      frontendUrl: FRONTEND_URL,
      apiUrl: API_URL,
      checks,
      findings
    };

    await ensureDir(ARTIFACT_DIR);
    const reportPath = path.join(ARTIFACT_DIR, 'ui-qa-report.json');
    await fs.writeFile(reportPath, JSON.stringify(report, null, 2));
    console.log(`UI QA report written to ${reportPath}`);
  } finally {
    if (api) {
      await api.dispose();
    }

    if (browser) {
      await browser.close();
    }
  }

  if (findings.length > 0) {
    process.exitCode = 1;
  }
}

run().catch(err => {
  console.error('UI QA run failed:', err);
  process.exit(1);
});
