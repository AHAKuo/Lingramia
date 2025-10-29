import { createRoot } from 'react-dom/client';
import App from './App';
import './styles.css';

declare const window: Window & typeof globalThis;

const container = document.getElementById('root');
if (!container) {
  throw new Error('Unable to find root container');
}

const root = createRoot(container);
root.render(<App />);
