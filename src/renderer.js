import React from 'react';
import { createRoot } from 'react-dom/client';
import App from './components/App';
import './index.css';

const container = document.getElementById('root');
const root = createRoot(container);
root.render(<App />);

console.log('Lingramia v1.0.0 - Renderer process started');
