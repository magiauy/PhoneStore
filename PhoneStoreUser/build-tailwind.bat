@echo off
cd /d "%~dp0"
echo Building Tailwind CSS...
node -e "const postcss = require('postcss'); const fs = require('fs'); const config = require('./postcss.config.js'); const css = fs.readFileSync('./wwwroot/app.css', 'utf8'); postcss(Object.keys(config.plugins).map(k => require(k)())).process(css, { from: './wwwroot/app.css', to: './wwwroot/app.min.css' }).then(result => { fs.writeFileSync('./wwwroot/app.min.css', result.css); console.log('✓ Tailwind CSS compiled successfully'); });"
pause
