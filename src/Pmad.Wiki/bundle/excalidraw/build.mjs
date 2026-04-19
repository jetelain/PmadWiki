import esbuild from 'esbuild';
import { copyFileSync, mkdirSync, readdirSync, statSync } from 'fs';
import { resolve, dirname, join } from 'path';
import { fileURLToPath } from 'url';

function copyDir(src, dest) {
    mkdirSync(dest, { recursive: true });
    for (const entry of readdirSync(src)) {
        const srcPath = join(src, entry);
        const destPath = join(dest, entry);
        if (statSync(srcPath).isDirectory()) {
            copyDir(srcPath, destPath);
        } else {
            copyFileSync(srcPath, destPath);
        }
    }
}

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(__dirname, '../../wwwroot/lib/excalidraw');

mkdirSync(outDir, { recursive: true });

// Bundle JS
await esbuild.build({
    entryPoints: [resolve(__dirname, 'entry.js')],
    bundle: true,
    format: 'esm',
    outfile: resolve(outDir, 'excalidraw-bundle.js'),
    minify: true,
    target: ['es2020'],
    define: {
        'process.env.NODE_ENV': '"production"',
    },
});

const prodDir = resolve(__dirname, 'node_modules/@excalidraw/excalidraw/dist/prod');
copyFileSync(resolve(prodDir, 'index.css'), resolve(outDir, 'excalidraw.css'));
copyDir(resolve(prodDir, 'fonts'), resolve(outDir, 'fonts'));

console.log(`Bundle written to ${outDir}`);
