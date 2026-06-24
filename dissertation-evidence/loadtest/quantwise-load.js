import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

// Per-endpoint latency trends (true = treat values as time)
const tProfile = new Trend('profile_ms', true);
const tPortfolio = new Trend('portfolio_ms', true);
const tNotifs = new Trend('notifications_ms', true);
const tRecs = new Trend('recommendations_ms', true);

const BASE = 'http://localhost:5000';
const TOKEN = __ENV.TOKEN;
const params = { headers: { Authorization: `Bearer ${TOKEN}` } };

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // ramp up
    { duration: '60s', target: 50 },  // hold at 50 virtual users
    { duration: '30s', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],          // <1% errors
    http_req_duration: ['p(95)<200'],         // NFR-01: reads under 200ms
    recommendations_ms: ['p(95)<200'],        // cached recommendation under 200ms
  },
};

export default function () {
  let r = http.get(`${BASE}/api/users/profile`, params);
  tProfile.add(r.timings.duration);
  check(r, { 'profile 200': (x) => x.status === 200 });

  r = http.get(`${BASE}/api/portfolios/me`, params);
  tPortfolio.add(r.timings.duration);
  check(r, { 'portfolio 200': (x) => x.status === 200 });

  r = http.get(`${BASE}/api/notifications`, params);
  tNotifs.add(r.timings.duration);
  check(r, { 'notifications 200': (x) => x.status === 200 });

  r = http.get(`${BASE}/api/recommendations`, params);
  tRecs.add(r.timings.duration);
  check(r, { 'recommendations 200': (x) => x.status === 200 });

  sleep(1);
}

function line(name, m) {
  if (!m) return `  ${name}: (no data)`;
  const v = m.values;
  const f = (x) => (x === undefined ? '-' : x.toFixed(1));
  return `  ${name.padEnd(18)} avg=${f(v.avg)}ms  med=${f(v.med)}ms  p95=${f(v['p(95)'])}ms  max=${f(v.max)}ms`;
}

export function handleSummary(data) {
  const m = data.metrics;
  const reqs = m.http_reqs ? m.http_reqs.values : { count: 0, rate: 0 };
  const fail = m.http_req_failed ? m.http_req_failed.values.rate : 0;
  const iters = m.iterations ? m.iterations.values.count : 0;
  const dur = m.http_req_duration ? m.http_req_duration.values : {};
  const vus = m.vus_max ? m.vus_max.values.max : 0;

  const txt = [
    '================ QuantWise k6 load test ================',
    `peak VUs:            ${vus}`,
    `iterations:          ${iters}`,
    `total HTTP requests: ${reqs.count}  (${reqs.rate.toFixed(1)} req/s)`,
    `failed requests:     ${(fail * 100).toFixed(2)}%`,
    `overall p95:         ${(dur['p(95)'] || 0).toFixed(1)} ms`,
    '--- per endpoint ---',
    line('profile', m.profile_ms),
    line('portfolios/me', m.portfolio_ms),
    line('notifications', m.notifications_ms),
    line('recommendations', m.recommendations_ms),
    '========================================================',
    '',
  ].join('\n');

  return {
    stdout: txt,
    'summary.json': JSON.stringify(data, null, 2),
  };
}
