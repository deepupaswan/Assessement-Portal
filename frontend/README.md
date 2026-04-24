# Real-Time Interview Assessment Frontend

Angular 17 frontend for the microservices-based Interview Assessment Platform.

## Implemented Features

- JWT auth flow (login/register) with token persistence.
- Role-based route protection (`Admin`, `Candidate`).
- Admin command center:
	- Create assessments
	- Assign assessments to candidates
	- Live candidate progress monitoring
	- Recent results and analytics snapshot
- Candidate workspace:
	- Assigned assessments list
	- Start/resume assessment session
	- Real-time countdown and warning updates via SignalR
	- Auto-save answers every few seconds
	- Auto-submit on timeout or violation threshold
- Anti-cheating hooks:
	- Tab switch detection (`visibilitychange`)
	- Fullscreen exit detection (`fullscreenchange`)
	- Suspicious activity reporting API call

## Frontend Structure

```
src/
	app/
		admin/
		auth/
		candidate/
		core/
			guards/
			interceptors/
			models/
			services/
		real-time/
		shared/
	environments/
	styles.scss
proxy.conf.json
```

## Route Map

- `/auth/login`
- `/auth/register`
- `/admin` (Admin role)
- `/candidate` (Candidate role)
- `/candidate/assessment/:candidateAssessmentId`

## Service Endpoints (via API Gateway)

The frontend calls gateway routes (proxied in development):

- `/api/auth/*`
- `/api/assessments/*`
- `/api/candidates/*`
- `/api/answers/*`
- `/api/results/*`
- `/hubs/assessment` (SignalR)

Update target URLs in `src/environments/environment.ts` and `proxy.conf.json` if your gateway runs on different ports.

## Local Setup

1. Start all backend microservices and Ocelot API gateway.
2. Ensure gateway is reachable (default configured target is `http://localhost:5000`).
3. Install frontend dependencies:

```bash
npm install
```

4. Start Angular dev server with proxy:

```bash
npm start
```

5. Open `http://localhost:4200`.

## Production Build

```bash
npm run build
```

Build output is generated in `dist/frontend`.
