window.hotkeyManager = {
    dotNetRef: null,
    
    initialize: function (dotNetReference) {
        this.dotNetRef = dotNetReference;
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        console.log('Hotkey manager initialized');
    },
    
    handleKeyDown: function (e) {
        // Ignore if user is typing in input/textarea
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
            return;
        }
        
        let key = '';
        
        // Build key combination string
        if (e.ctrlKey) key += 'ctrl+';
        if (e.altKey) key += 'alt+';
        if (e.shiftKey) key += 'shift+';
        key += e.key.toLowerCase();
        
        console.log('Hotkey pressed:', key);
        
        // Prevent default for registered hotkeys
        const registeredKeys = [
            'ctrl+h', 'ctrl+b', 'ctrl+m', 'ctrl+c', 
            'ctrl+d', 'ctrl+s', 'ctrl+e', 'ctrl+/',
            'alt+1', 'alt+2', 'alt+3', 'alt+4',
            // Admin & Manager hotkeys
            'alt+a', 'alt+b', 'alt+r', 'alt+e', 'alt+n'
        ];
        
        if (registeredKeys.includes(key)) {
            e.preventDefault();
            this.dotNetRef.invokeMethodAsync('TriggerHotkey', key);
        }
    },
    
    dispose: function () {
        document.removeEventListener('keydown', this.handleKeyDown);
        this.dotNetRef = null;
    }
};

