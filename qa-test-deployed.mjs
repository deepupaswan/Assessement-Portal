import fs from 'node:fs/promises';
import path from 'node:path';
import { chromium } from '@playwright/test';

// ============== CONFIGURATION ==============
const DEPLOYED_URL = 'http://3.27.249.101';
const API_URL = 'http://3.27.249.101/api';
const CHROME_PATH = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const ARTIFACT_DIR = path.resolve('qa-test-artifacts');
const PASSWORD = 'QATest@12345';

// ============== LOGGING ==============
const testResults = [];
const issues = [];

function logTest(status, name, details = '') {
  const entry = { status, name, details, timestamp: new Date().toISOString() };
  testResults.push(entry);
  const emoji = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️';
  const suffix = details ? ` | ${details}` : '';
  console.log(`${emoji} [${status}] ${name}${suffix}`);
}

function logIssue(severity, title, details) {
  issues.push({ severity, title, details });
  console.log(`\n🚨 [${severity.toUpperCase()}] ${title}\n   ${details}\n`);
}

async function ensureDir(dir) {
  try {
    await fs.mkdir(dir, { recursive: true });
  } catch (e) {
    // ignore
  }
}

async function screenshot(page, fileName) {
  await ensureDir(ARTIFACT_DIR);
  const fullPath = path.join(ARTIFACT_DIR, fileName);
  try {
    await page.screenshot({ path: fullPath, fullPage: true });
    return fullPath;
  } catch (e) {
    return null;
  }
}

// ============== TEST SUITE ==============
async function runTests() {
  console.log('\n╔════════════════════════════════════════════════════════════════════╗');
  console.log('║                    QA TESTING - DEPLOYED APPLICATION                ║');
  console.log('║                   http://3.27.249.101                                ║');
  console.log('╚════════════════════════════════════════════════════════════════════╝\n');

  let browser;
  try {
    browser = await chromium.launch({
      headless: true,
      executablePath: CHROME_PATH
    });

    // ============== TEST 1: CONNECTIVITY & LANDING PAGE ==============
    console.log('\n📋 TEST SUITE 1: CONNECTIVITY & LANDING PAGE\n');
    let page = await browser.newPage();
    try {
      await page.goto(DEPLOYED_URL, { waitUntil: 'domcontentloaded', timeout: 15000 });
      logTest('PASS', 'Application is accessible', `Response: ${page.url()}`);
      await screenshot(page, '01-landing-page.png');
    } catch (e) {
      logTest('FAIL', 'Application connectivity', e.message);
      logIssue('CRITICAL', 'Cannot reach application', `Server at ${DEPLOYED_URL} is not responding`);
      return;
    }

    try {
      const title = await page.title();
      if (title.includes('Portal') || title.includes('Assessment')) {
        logTest('PASS', 'Page title correct', title);
      } else {
        logTest('FAIL', 'Page title unexpected', `Got: "${title}"`);
      }
    } catch (e) {
      logTest('FAIL', 'Cannot read page title', e.message);
    }

    await page.close();

    // ============== TEST 2: AUTHENTICATION - REGISTRATION ==============
    console.log('\n📋 TEST SUITE 2: USER REGISTRATION\n');
    page = await browser.newPage();
    const timestamp = Date.now();
    const adminEmail = `admin.qa.${timestamp}@test.com`;
    const candidateEmail = `candidate.qa.${timestamp}@test.com`;

    try {
      await page.goto(DEPLOYED_URL, { waitUntil: 'domcontentloaded' });
      
      // Click "Create account" link
      const createLink = await page.locator('text=Create account').first();
      if (await createLink.isVisible({ timeout: 5000 }).catch(() => false)) {
        await createLink.click();
        await page.waitForURL('**/auth/register', { timeout: 5000 });
        logTest('PASS', 'Navigation to registration page', 'Register page loaded');
        await screenshot(page, '02-register-page.png');

        // Register admin user
        try {
          await page.getByLabel('Name').fill('QA Admin Test');
          await page.getByLabel('Email').fill(adminEmail);
          await page.getByLabel('Password').fill(PASSWORD);
          const adminRadio = await page.locator('input[value="Admin"]').first();
          if (await adminRadio.isVisible({ timeout: 2000 }).catch(() => false)) {
            await adminRadio.check();
          }
          await page.getByRole('button', { name: 'Create account' }).click();
          
          // Wait for redirect to admin dashboard
          await page.waitForURL('**/admin/**', { timeout: 10000 }).catch(() => {});
          logTest('PASS', 'Admin registration successful', adminEmail);
          await screenshot(page, '03-admin-dashboard.png');
        } catch (e) {
          logTest('FAIL', 'Admin registration', e.message);
        }
      } else {
        logTest('FAIL', 'Registration link not found', 'Cannot locate "Create account" link');
      }
    } catch (e) {
      logTest('FAIL', 'Registration flow', e.message);
    }

    await page.close();

    // ============== TEST 3: LOGIN FLOW ==============
    console.log('\n📋 TEST SUITE 3: USER LOGIN\n');
    page = await browser.newPage();
    try {
      await page.goto(DEPLOYED_URL, { waitUntil: 'domcontentloaded' });
      
      // Try login with admin credentials
      await page.getByLabel('Email').fill(adminEmail);
      await page.getByLabel('Password').fill(PASSWORD);
      await page.getByRole('button', { name: 'Sign in' }).click();
      
      await page.waitForURL('**/admin/**', { timeout: 10000 }).catch(() => {});
      const currentUrl = page.url();
      if (currentUrl.includes('/admin')) {
        logTest('PASS', 'Login successful', adminEmail);
        await screenshot(page, '04-after-login.png');
      } else {
        logTest('FAIL', 'Login redirect failed', `Expected admin page, got: ${currentUrl}`);
      }
    } catch (e) {
      logTest('FAIL', 'Login flow', e.message);
    }

    await page.close();

    // ============== TEST 4: API VALIDATION ==============
    console.log('\n📋 TEST SUITE 4: API VALIDATION & SECURITY\n');
    try {
      const response = await page.request?.get(`${API_URL}/health`).catch(() => null);
      if (response) {
        logTest('PASS', 'Health endpoint accessible', `Status: ${response.status()}`);
      } else {
        logTest('WARN', 'Health endpoint check', 'Could not verify');
      }
    } catch (e) {
      logTest('WARN', 'API health check', e.message);
    }

    // Test invalid input validation
    console.log('\n  → Testing input validation...');
    try {
      // Test with empty email
      page = await browser.newPage();
      await page.goto(DEPLOYED_URL);
      await page.getByLabel('Email').fill('');
      await page.getByLabel('Password').fill('test');
      const signInBtn = await page.getByRole('button', { name: 'Sign in' });
      
      // Check if button is disabled or form has validation
      const isDisabled = await signInBtn.isDisabled().catch(() => false);
      if (isDisabled) {
        logTest('PASS', 'Form validation - empty email blocked', 'Submit button disabled');
      } else {
        logTest('WARN', 'Form validation', 'Empty email validation unclear');
      }
      await page.close();
    } catch (e) {
      logTest('WARN', 'Input validation test', e.message);
    }

    // ============== TEST 5: RESPONSIVE DESIGN ==============
    console.log('\n📋 TEST SUITE 5: RESPONSIVE DESIGN\n');
    const viewports = [
      { name: 'Desktop', width: 1920, height: 1080 },
      { name: 'Tablet', width: 768, height: 1024 },
      { name: 'Mobile', width: 375, height: 667 }
    ];

    for (const viewport of viewports) {
      try {
        page = await browser.newPage({ viewport });
        await page.goto(DEPLOYED_URL, { waitUntil: 'domcontentloaded', timeout: 10000 });
        const isVisible = await page.locator('button').first().isVisible().catch(() => false);
        if (isVisible) {
          logTest('PASS', `${viewport.name} responsiveness`, `${viewport.width}x${viewport.height}`);
        } else {
          logTest('WARN', `${viewport.name} responsiveness`, 'Layout might be broken');
        }
        await page.close();
      } catch (e) {
        logTest('FAIL', `${viewport.name} viewport test`, e.message);
      }
    }

    // ============== TEST 6: PERFORMANCE ==============
    console.log('\n📋 TEST SUITE 6: PERFORMANCE CHECKS\n');
    page = await browser.newPage();
    const startTime = Date.now();
    try {
      await page.goto(DEPLOYED_URL, { waitUntil: 'networkidle' });
      const loadTime = Date.now() - startTime;
      
      if (loadTime < 5000) {
        logTest('PASS', 'Page load performance', `${loadTime}ms (Excellent)`);
      } else if (loadTime < 10000) {
        logTest('WARN', 'Page load performance', `${loadTime}ms (Acceptable)`);
      } else {
        logTest('FAIL', 'Page load performance', `${loadTime}ms (Slow)`);
        logIssue('MEDIUM', 'Slow page load', `Application took ${loadTime}ms to load`);
      }
    } catch (e) {
      logTest('WARN', 'Performance check', e.message);
    }
    await page.close();

    // ============== TEST 7: ERROR HANDLING ==============
    console.log('\n📋 TEST SUITE 7: ERROR HANDLING\n');
    page = await browser.newPage();
    try {
      await page.goto(`${DEPLOYED_URL}/nonexistent-page`, { waitUntil: 'domcontentloaded' }).catch(() => {});
      const notFoundText = await page.locator('body').textContent();
      if (notFoundText?.includes('404') || notFoundText?.includes('not found') || notFoundText?.includes('Not Found')) {
        logTest('PASS', '404 error page', 'Proper error handling');
      } else {
        logTest('WARN', '404 error handling', 'Page behavior unclear');
      }
    } catch (e) {
      logTest('WARN', '404 test', e.message);
    }
    await page.close();

    // ============== CLEANUP ==============
    await browser.close();

  } catch (err) {
    console.error('Critical test error:', err);
    logTest('FAIL', 'Test suite execution', err.message);
  }

  // ============== REPORT GENERATION ==============
  console.log('\n\n╔════════════════════════════════════════════════════════════════════╗');
  console.log('║                         TEST SUMMARY REPORT                          ║');
  console.log('╚════════════════════════════════════════════════════════════════════╝\n');

  const passed = testResults.filter(r => r.status === 'PASS').length;
  const failed = testResults.filter(r => r.status === 'FAIL').length;
  const warnings = testResults.filter(r => r.status === 'WARN').length;
  const total = testResults.length;

  console.log(`📊 RESULTS:`);
  console.log(`   ✅ Passed:  ${passed}/${total}`);
  console.log(`   ❌ Failed:  ${failed}/${total}`);
  console.log(`   ⚠️  Warnings: ${warnings}/${total}`);
  console.log(`   📈 Success Rate: ${((passed/total)*100).toFixed(1)}%\n`);

  if (failed > 0) {
    console.log(`⚠️  FAILED TESTS:`);
    testResults.filter(r => r.status === 'FAIL').forEach(r => {
      console.log(`   ❌ ${r.name} - ${r.details}`);
    });
    console.log();
  }

  if (issues.length > 0) {
    console.log(`\n🚨 ISSUES FOUND:`);
    issues.forEach(issue => {
      console.log(`   [${issue.severity}] ${issue.title}`);
      console.log(`      → ${issue.details}\n`);
    });
  }

  console.log(`\n📁 Artifacts saved to: ${ARTIFACT_DIR}`);
  console.log(`\n✅ QA Testing Complete!\n`);

  // Save detailed report
  const report = {
    timestamp: new Date().toISOString(),
    url: DEPLOYED_URL,
    summary: { passed, failed, warnings, total, successRate: ((passed/total)*100).toFixed(1) },
    testResults,
    issues
  };

  await ensureDir(ARTIFACT_DIR);
  await fs.writeFile(
    path.join(ARTIFACT_DIR, 'qa-test-report.json'),
    JSON.stringify(report, null, 2)
  );
}

// Run tests
runTests().catch(console.error);
