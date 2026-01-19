// Скрипт для проверки JWT токена в консоли браузера
// Использование: просто вызови checkToken() в консоли

function checkToken() {
    const token = localStorage.getItem('authToken');
    
    if (!token) {
        console.error('❌ Токен не найден в localStorage');
        return;
    }
    
    console.log('✅ Токен найден, длина:', token.length);
    
    try {
        // Декодируем JWT токен (без проверки подписи)
        const parts = token.split('.');
        if (parts.length !== 3) {
            console.error('❌ Неверный формат токена');
            return;
        }
        
        // Декодируем payload (вторая часть)
        const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
        
        console.log('📋 Payload токена:');
        console.log(payload);
        
        // Проверяем роли
        const roles = [];
        
        // Проверяем разные варианты хранения ролей
        if (payload.role) {
            roles.push(payload.role);
        }
        if (payload.roles && Array.isArray(payload.roles)) {
            roles.push(...payload.roles);
        }
        if (payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']) {
            const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
            if (Array.isArray(roleClaim)) {
                roles.push(...roleClaim);
            } else {
                roles.push(roleClaim);
            }
        }
        
        // Проверяем все claims
        console.log('\n🔍 Поиск ролей в claims:');
        for (const [key, value] of Object.entries(payload)) {
            if (key.toLowerCase().includes('role')) {
                console.log(`  ${key}:`, value);
            }
        }
        
        console.log('\n👤 Роли пользователя:');
        if (roles.length > 0) {
            roles.forEach(role => {
                const isManager = role === 'Manager' || role === 'manager' || role.toLowerCase() === 'manager';
                console.log(`  ${isManager ? '✅' : '  '} ${role}`);
            });
            
            const hasManager = roles.some(r => r === 'Manager' || r === 'manager' || r.toLowerCase() === 'manager');
            if (hasManager) {
                console.log('\n✅ Роль Manager найдена в токене!');
            } else {
                console.log('\n❌ Роль Manager НЕ найдена в токене!');
                console.log('   Это означает, что проблема в генерации JWT токена на стороне API.');
                console.log('   Нужно проверить метод Login в AuthController.');
            }
        } else {
            console.log('  ❌ Роли не найдены в токене!');
            console.log('   Это означает, что проблема в генерации JWT токена на стороне API.');
        }
        
        // Проверяем другие важные поля
        console.log('\n📝 Другие поля:');
        if (payload.sub || payload.nameid) {
            console.log('  User ID:', payload.sub || payload.nameid);
        }
        if (payload.email) {
            console.log('  Email:', payload.email);
        }
        if (payload.exp) {
            const expDate = new Date(payload.exp * 1000);
            console.log('  Expires:', expDate.toLocaleString());
            if (expDate < new Date()) {
                console.log('  ⚠️ Токен истек!');
            }
        }
        
    } catch (error) {
        console.error('❌ Ошибка при декодировании токена:', error);
    }
}

// Автоматически вызываем при загрузке
if (typeof window !== 'undefined') {
    window.checkToken = checkToken;
    console.log('💡 Для проверки токена вызови: checkToken()');
}
