/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Views/**/*.cshtml",
        "./Views/**/*.razor",
        "./Areas/**/*.cshtml",
        "./Pages/**/*.cshtml", 
        "./wwwroot/**/*.js",
        "./wwwroot/**/*.html"
    ],
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
                mono: ['Consolas', 'Monaco', 'Courier New', 'monospace'],
            },
        },
    },
    plugins: [],
}