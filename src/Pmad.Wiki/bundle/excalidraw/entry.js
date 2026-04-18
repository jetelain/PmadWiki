// Entry point: re-exports everything the page needs from a single ESM bundle.
// React is exported alongside Excalidraw so both share the same instance.

export { default as React, useState, useEffect, useRef } from 'react';
export { createRoot } from 'react-dom/client';
export { default as ReactDOM } from 'react-dom';
export * as ExcalidrawLib from '@excalidraw/excalidraw';
