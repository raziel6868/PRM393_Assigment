import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { readFileSync, existsSync } from 'node:fs';
import path from 'node:path';

const envDir = path.resolve(__dirname, '..');

/**
 * Read a single key from the repository-root .env file.
 * Vite's loadEnv merges process.env on top of the file, which means a leaked
 * shell value can silently override the authoritative repository value.
 * This helper bypasses that merge so the .env file is the single source of
 * truth for VITE_API_BASE_URL.
 */
function readEnvValue(key: string): string | undefined {
  const filePath = path.join(envDir, '.env');
  if (!existsSync(filePath)) return undefined;
  const text = readFileSync(filePath, 'utf8');
  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;
    const eq = line.indexOf('=');
    if (eq <= 0) continue;
    const name = line.slice(0, eq).trim();
    if (name !== key) continue;
    let value = line.slice(eq + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    return value;
  }
  return undefined;
}

const fileApiBaseUrl = readEnvValue('VITE_API_BASE_URL');
const apiBaseUrl = fileApiBaseUrl ?? loadEnv(process.env.APP_ENV ?? 'development', envDir, '').VITE_API_BASE_URL;

if (!apiBaseUrl) {
  throw new Error('VITE_API_BASE_URL is not set. Configure it in the repository-root .env before building.');
}

if (process.env.VITE_API_BASE_URL && process.env.VITE_API_BASE_URL !== apiBaseUrl) {
  // Surface the conflict so the developer can clean up the leak instead of
  // being surprised by silently-wrong API URLs in production bundles.
  // eslint-disable-next-line no-console
  console.warn(
    `[vite.config] Ignoring leaked process.env.VITE_API_BASE_URL='${process.env.VITE_API_BASE_URL}' and using file value '${apiBaseUrl}'.`,
  );
}

export default defineConfig({
  envDir,
  plugins: [react()],
  resolve: {
    alias: {
      '@app': path.resolve(__dirname, 'src/app'),
      '@api': path.resolve(__dirname, 'src/api'),
      '@features': path.resolve(__dirname, 'src/features'),
      '@shared': path.resolve(__dirname, 'src/shared'),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    host: 'localhost',
    proxy: {
      '/api/v1': {
        target: process.env.QA_API_PROXY_TARGET ?? 'http://127.0.0.1:5080',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  preview: {
    port: 5173,
    strictPort: true,
    host: 'localhost',
    proxy: {
      '/api/v1': {
        target: process.env.QA_API_PROXY_TARGET ?? 'http://127.0.0.1:5080',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    target: 'es2022',
    sourcemap: false,
  },
  define: {
    __API_BASE_URL__: JSON.stringify(apiBaseUrl),
  },
});
