const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const morgan = require('morgan');
const path = require('path');
const fs = require('fs');
const http = require('http');
const zlib = require('zlib');
const os = require('os');
const rateLimit = require('express-rate-limit');
require('dotenv').config();

const app = express();

// Validate PORT and HOST
const rawPort = process.env.PORT || 3000;
const parsed = Math.floor(Number(rawPort)) || 3000;
const PORT = Math.min(65535, Math.max(1, parsed));
if (parsed !== PORT) {
  console.warn(`PORT ${rawPort} adjusted to ${PORT}`);
}
const HOST = process.env.HOST || '0.0.0.0';

const publicDir = path.join(__dirname, 'public');
const indexPath = path.join(publicDir, 'index.html');
if (!fs.existsSync(publicDir) || !fs.existsSync(indexPath)) {
  console.error('Missing public/ or public/index.html. Server may not serve the game correctly.');
}

const isDev = process.env.NODE_ENV !== 'production';

// Middleware
app.use(helmet({
  contentSecurityPolicy: false, // Disable CSP for Unity WebGL
}));
app.use(cors({
  origin: isDev ? true : (process.env.CORS_ORIGIN || '*').split(',').map(s => s.trim()),
  credentials: true,
}));
app.use(morgan('combined'));
app.use(express.json({ limit: '1mb' }));
app.use(express.urlencoded({ extended: true, limit: '1mb' }));

app.use('/api/', rateLimit({
  windowMs: 60 * 1000,
  max: 100,
  standardHeaders: true,
  legacyHeaders: false,
}));

// Serve decompressed Unity files (mobile-friendly, no Brotli decode on client)
const DECOMPRESS_MAP = {
  'build1.framework.js': 'build1.framework.js.br',
  'build1.data': 'build1.data.br',
  'build1.wasm': 'build1.wasm.br',
  'build2.framework.js': 'build2.framework.js.br',
  'build2.data': 'build2.data.br',
  'build2.wasm': 'build2.wasm.br',
};
app.get('/Build/:file', (req, res, next) => {
  const file = path.basename(req.params.file);
  const brFile = DECOMPRESS_MAP[file];
  if (!brFile) return next();
  const filePath = path.join(publicDir, 'Build', brFile);
  let buf;
  try {
    buf = zlib.brotliDecompressSync(fs.readFileSync(filePath));
  } catch {
    return next();
  }
  res.setHeader('Content-Length', buf.length);
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  if (file.endsWith('.js')) res.setHeader('Content-Type', 'application/javascript');
  else if (file.endsWith('.wasm')) res.setHeader('Content-Type', 'application/wasm');
  else if (file.endsWith('.data')) res.setHeader('Content-Type', 'application/octet-stream');
  res.end(buf);
});

// Serve .br files (fallback for direct .br requests)
const BR_FILES = [
  'build1.framework.js.br', 'build1.wasm.br', 'build1.data.br',
  'build2.framework.js.br', 'build2.wasm.br', 'build2.data.br',
];
app.get('/Build/:file', (req, res, next) => {
  const file = path.basename(req.params.file);
  if (!BR_FILES.includes(file)) return next();
  const filePath = path.join(publicDir, 'Build', file);
  let buf;
  try {
    buf = fs.readFileSync(filePath);
  } catch {
    return next();
  }
  res.setHeader('Content-Encoding', 'br');
  res.setHeader('Content-Length', buf.length);
  res.setHeader('Vary', 'Accept-Encoding');
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  if (file.endsWith('.js.br')) res.setHeader('Content-Type', 'application/javascript');
  else if (file.endsWith('.wasm.br')) res.setHeader('Content-Type', 'application/wasm');
  else if (file.endsWith('.data.br')) res.setHeader('Content-Type', 'application/octet-stream');
  res.end(buf);
});

// Serve all other static files (TemplateData, Build/loader.js, etc.) with COEP/COOP
app.use(express.static(publicDir, {
  setHeaders: (res, filePath) => {
    const p = filePath.replace(/\\/g, '/');
    if (p.endsWith('.br')) {
      res.setHeader('Content-Encoding', 'br');
      if (p.endsWith('.js.br')) res.setHeader('Content-Type', 'application/javascript');
      else if (p.endsWith('.wasm.br')) res.setHeader('Content-Type', 'application/wasm');
      else if (p.endsWith('.data.br')) res.setHeader('Content-Type', 'application/octet-stream');
    }
    res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
    res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  },
}));

// Routes
app.get('/', (req, res) => {
  res.sendFile(indexPath);
});

app.get('/api/health', (req, res) => {
  res.json({
    message: 'Express.js Server is running!',
    timestamp: new Date().toISOString(),
    version: '1.0.0',
  });
});

// 404
app.use((req, res) => {
  res.status(404).json({ error: 'Not Found', path: req.path });
});

// Global error handler
app.use((err, req, res, next) => {
  console.error(err.stack || err);
  res.status(500).json({
    error: 'Internal Server Error',
    message: isDev ? err.message : undefined,
  });
});

// Start server
const server = app.listen(PORT, HOST, () => {
  console.log(`🚀 Server running on http://${HOST}:${PORT}`);
  console.log(`🎮 Unity Game (this computer): http://localhost:${PORT}`);
  const ifaces = os.networkInterfaces();
  const ips = [];
  for (const name of Object.keys(ifaces)) {
    for (const iface of ifaces[name]) {
      if (iface.family === 'IPv4' && !iface.internal) ips.push(iface.address);
    }
  }
  if (ips.length) {
    console.log(`📱 On your phone (same Wi‑Fi): http://${ips[0]}:${PORT}`);
  }
  console.log(`📊 Health check: http://${HOST}:${PORT}/api/health`);
  console.log(`🌍 Environment: ${process.env.NODE_ENV || 'development'}`);

  const checkHost = HOST === '0.0.0.0' ? '127.0.0.1' : HOST;
  const req = http.get(`http://${checkHost}:${PORT}/api/health`, { timeout: 5000 }, (res) => {
    let body = '';
    res.on('data', (chunk) => { body += chunk; });
    res.on('end', () => {
      if (res.statusCode === 200 && body.includes('Express.js Server is running')) {
        console.log('✅ Startup self-check OK — server responding on port');
      } else {
        console.error(`❌ Startup self-check FAILED — health returned ${res.statusCode}`);
        process.exit(1);
      }
    });
  });
  req.on('error', (err) => {
    console.error('❌ Startup self-check FAILED — could not reach server:', err.message);
    process.exit(1);
  });
  req.on('timeout', () => {
    req.destroy();
    console.error('❌ Startup self-check FAILED — request timed out');
    process.exit(1);
  });
});

server.on('error', (err) => {
  if (err.code === 'EADDRINUSE') {
    console.error(`Port ${PORT} is already in use. Choose another PORT (e.g. in .env).`);
  } else {
    console.error('Server error:', err);
  }
  process.exit(1);
});

function shutdown(signal) {
  console.log(`${signal} received, shutting down gracefully`);
  server.close((err) => {
    if (err) {
      console.error('Error closing server:', err);
      process.exit(1);
    }
    process.exit(0);
  });
  setTimeout(() => {
    console.error('Forced exit after 10s');
    process.exit(1);
  }, 10000);
}

process.on('SIGTERM', () => shutdown('SIGTERM'));
process.on('SIGINT', () => shutdown('SIGINT'));
