const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

function startDetached(command, args, cwd) {
  try {
    const logPath = path.join(cwd || process.cwd(), 'backend.log');
    if (process.platform === 'win32') {
      // Windows: spawn detached with stdout/stderr to log file
      const logStream = fs.openSync(logPath, 'a');
      const child = spawn(command, args || [], {
        cwd: cwd || process.cwd(),
        detached: true,
        stdio: ['ignore', logStream, logStream]
      });
      child.unref();
      console.log(`Started detached (logs -> ${logPath}): ${command}`);
    } else {
      // Unix-like: use nohup and sh to background with redirection
      const cmd = `nohup "${command}" ${args ? args.map(a => `${a}`).join(' ') : ''} > "${logPath}" 2>&1 &`;
      const child = spawn('sh', ['-c', cmd], { cwd: cwd || process.cwd(), detached: true, stdio: 'ignore' });
      child.unref();
      console.log(`Started detached (logs -> ${logPath}): ${command}`);
    }
    // Do not exit here — caller will wait for readiness when needed
    return true;
  } catch (err) {
    console.error('Failed to start detached process:', err);
    return false;
  }
}

function startAttached(command, args, cwd) {
  try {
    const child = spawn(command, args || [], {
      cwd: cwd || process.cwd(),
      stdio: 'inherit'
    });
    child.on('exit', (code, signal) => {
      if (signal) process.exit(1);
      process.exit(code || 0);
    });
  } catch (err) {
    console.error('Failed to start attached process:', err);
    process.exit(1);
  }
}

const repoRoot = path.resolve(__dirname, '..');
const exePath = path.join(repoRoot, 'backend', 'MWDMockServer.exe');
const netExePath = path.join(repoRoot, 'backend', 'net9.0', 'MWDMockServer.exe');
const clientApiDir = path.join(repoRoot, 'backend', 'ClientAPIServer');

const attachRequested = process.argv.includes('--attach') || process.env.BACKEND_ATTACH === '1' || process.env.BACKEND_ATTACH === 'true';

function httpGet(url, timeout = 2000) {
  return new Promise((resolve, reject) => {
    try {
      const lib = url.startsWith('https') ? require('https') : require('http');
      const req = lib.get(url, { timeout }, (res) => {
        // consider server up if we got any response (200-499)
        const ok = res.statusCode && res.statusCode < 500;
        res.resume();
        resolve(ok);
      });
      req.on('error', () => resolve(false));
      req.on('timeout', () => {
        req.destroy();
        resolve(false);
      });
    } catch (e) {
      resolve(false);
    }
  });
}

function waitForAny(urls, opts = {}) {
  const timeout = opts.timeout || 30000;
  const interval = opts.interval || 1000;
  const start = Date.now();
  return new Promise((resolve, reject) => {
    (async function poll() {
      for (const u of urls) {
        // eslint-disable-next-line no-await-in-loop
        if (await httpGet(u)) return resolve(true);
      }
      if (Date.now() - start >= timeout) return resolve(false);
      setTimeout(poll, interval);
    })();
  });
}

// Prefer the published exe under backend/net9.0 if present, then root backend exe, then source folder
(async function run() {
  if (fs.existsSync(netExePath)) {
    if (attachRequested) {
      startAttached(netExePath, [], path.dirname(netExePath));
      return;
    } else {
      const started = startDetached(netExePath, [], path.dirname(netExePath));
      if (!started) process.exit(1);
      // Wait a bit for process to start
      console.log('Waiting for backend to start...');
      await new Promise(r => setTimeout(r, 2000));
      // Wait for readiness
      const healthEnv = process.env.BACKEND_HEALTH_URL;
      const urls = healthEnv ? healthEnv.split(',') : [
        'http://localhost:8357/',
        'http://localhost:5001/',
        'http://localhost:8357/api/v3/',
        'http://localhost:5001/api/v3/'
      ];
      const ok = await waitForAny(urls);
      if (!ok) {
        console.warn('Backend did not become ready within timeout; check backend logs.');
        process.exit(1);
      }
      console.log('Backend is responding.');
      process.exit(0);
    }
  } else if (fs.existsSync(exePath)) {
    if (attachRequested) {
      startAttached(exePath, [], path.dirname(exePath));
      return;
    } else {
      const started = startDetached(exePath, [], path.dirname(exePath));
      if (!started) process.exit(1);
      console.log('Waiting for backend to start...');
      await new Promise(r => setTimeout(r, 2000));
      const healthEnv = process.env.BACKEND_HEALTH_URL;
      const urls = healthEnv ? healthEnv.split(',') : [
        'http://localhost:8357/',
        'http://localhost:5001/',
        'http://localhost:8357/api/v3/',
        'http://localhost:5001/api/v3/'
      ];
      const ok = await waitForAny(urls);
      if (!ok) {
        console.warn('Backend did not become ready within timeout; check backend logs.');
        process.exit(1);
      }
      console.log('Backend is responding.');
      process.exit(0);
    }
  } else if (fs.existsSync(clientApiDir)) {
    // Fallback to previous dotnet run behavior
    if (attachRequested) {
      startAttached('dotnet', ['run'], clientApiDir);
      return;
    } else {
      const started = startDetached('dotnet', ['run'], clientApiDir);
      if (!started) process.exit(1);
      console.log('Waiting for dotnet backend to start...');
      await new Promise(r => setTimeout(r, 3000));
      const healthEnv = process.env.BACKEND_HEALTH_URL;
      const urls = healthEnv ? healthEnv.split(',') : [
        'http://localhost:8357/',
        'http://localhost:5001/',
        'http://localhost:8357/api/v3/',
        'http://localhost:5001/api/v3/'
      ];
      const ok = await waitForAny(urls);
      if (!ok) {
        console.warn('Backend did not become ready within timeout; check backend logs.');
        process.exit(1);
      }
      console.log('Backend is responding.');
      process.exit(0);
    }
  } else {
    console.error('No backend found. Place MWDMockServer.exe in backend/ or ensure backend/ClientAPIServer exists.');
    process.exit(1);
  }
})();
