namespace Tailwind;

public static class TailwindConfigTemplates
{
    public const string Basic = """
                /** @type {import('tailwindcss').Config} */
        module.exports = {
            content: ["./Pages/**/*.{html,js,cshtml,razor}", "./Shared/**/*.{html,js,cshtml,razor}"],
            plugins: [],
        }
        """;

    public const string Batteries = """
                /** @type {import('tailwindcss').Config} */
        const defaultTheme = require('tailwindcss/defaultTheme')

        module.exports = {
            content: ["./Pages/**/*.{html,js,cshtml,razor}", "./Shared/**/*.{html,js,cshtml,razor}"],
            theme: {
                extend: {
                    fontFamily: {
                        sans: ['Inter var', ...defaultTheme.fontFamily.sans],
                    },
                },
            },
            plugins: [
                require('@tailwindcss/forms'),
                require('@tailwindcss/typography'),
            ],
        }
        """;
}