# QuantWise Mobile

Native iOS/Android app (Expo + React Native + TypeScript) for the QuantWise
goal-based robo-advisory. It reuses the same REST API as the web frontend — the
UI is rebuilt natively; the backend contract is shared.

## Stack

- **Expo SDK 57** / React Native 0.86 / React 19
- **React Navigation** — native stack (auth) + bottom tabs (main)
- **TanStack Query** — server state, mirrors the web retry policy
- **expo-secure-store** — the auth token lives in the OS keychain (falls back to
  localStorage on Expo web for local preview only)
- Theme, i18n (EN/AR + RTL) and API contract ported from `../frontend`

## Screens

| Flow | Screen |
|------|--------|
| Auth | Login, Signup (`/api/users/*`) |
| Suitability | 10-question Onboarding — raw answers only, scoring is server-side (§2) |
| Home | Dashboard — profile tiles, AI recommendations (Gemini), honest track record |
| Plan | Generate → review → accept proposal (deterministic optimizer, §3.3), live mark-to-market portfolio card |
| Account | Profile — risk summary, language toggle, sign out |

## Running locally

The app talks to the local backend (default `http://localhost:5099`; Android
emulator uses `http://10.0.2.2:5099`). Override with `EXPO_PUBLIC_API_URL`.

```bash
cd mobile
npm install
npm run web      # preview in a browser (react-native-web)
npm run ios      # iOS simulator (macOS)
npm run android  # Android emulator
```

For a real device, run `npx expo start`, set `EXPO_PUBLIC_API_URL` to your
machine's LAN IP, and scan the QR code with Expo Go.

## Notes

- RTL: on web the layout direction flips live; on a native device
  `I18nManager.forceRTL` needs an app reload to fully re-mirror.
- The brand display/mono fonts aren't bundled yet — the app uses the platform
  system + monospace families. Bundling `Martian Mono` / `Spline Sans Mono` via
  `expo-font` is the main visual polish item.
