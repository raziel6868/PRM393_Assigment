import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './app/router';
import 'antd/dist/reset.css';

const root = document.getElementById('root');
if (!root) {
  throw new Error('Root container not found.');
}

ReactDOM.createRoot(root).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
