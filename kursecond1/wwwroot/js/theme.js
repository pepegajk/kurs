// Функция для определения системной темы
function getSystemTheme() {
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    }
    return 'light';
}

// Функция для применения темы
window.applyTheme = function (theme) {
    const html = document.documentElement;
    
    if (theme === 'System') {
        const systemTheme = getSystemTheme();
        if (systemTheme === 'dark') {
            html.setAttribute('data-theme', 'dark');
        } else {
            html.removeAttribute('data-theme');
        }
    } else if (theme === 'Dark') {
        html.setAttribute('data-theme', 'dark');
    } else {
        html.removeAttribute('data-theme');
    }
};

// Слушаем изменения системной темы для автоматического обновления при выборе "Системная"
if (window.matchMedia) {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    // Функция для обработки изменений системной темы
    function handleSystemThemeChange(e) {
        // Проверяем, если текущая тема - "Системная", применяем изменения
        // Это будет обрабатываться через Blazor при изменении настроек
        const event = new CustomEvent('systemThemeChanged', { detail: { isDark: e.matches } });
        window.dispatchEvent(event);
    }
    
    // Добавляем слушатель для современных браузеров
    if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', handleSystemThemeChange);
    } else {
        // Для старых браузеров
        mediaQuery.addListener(handleSystemThemeChange);
    }
}

