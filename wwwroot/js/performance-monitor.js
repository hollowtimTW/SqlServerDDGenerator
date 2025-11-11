// 效能監控工具
(function() {
    'use strict';
    
    // 只在開發環境啟用
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        
        // FPS 監控
        let lastTime = performance.now();
        let frames = 0;
        let fps = 0;
        
        function measureFPS() {
            frames++;
            const currentTime = performance.now();
            if (currentTime >= lastTime + 1000) {
                fps = Math.round((frames * 1000) / (currentTime - lastTime));
                frames = 0;
                lastTime = currentTime;
                
                // 如果 FPS 低於 30，在 console 警告
                if (fps < 30) {
                    console.warn(`⚠️ Low FPS detected: ${fps} FPS`);
                }
            }
            requestAnimationFrame(measureFPS);
        }
        
        // 記憶體使用監控（Chrome only）
        function logMemoryUsage() {
            if (performance.memory) {
                const used = Math.round(performance.memory.usedJSHeapSize / 1048576);
                const total = Math.round(performance.memory.jsHeapSizeLimit / 1048576);
                console.log(`📊 Memory: ${used}MB / ${total}MB (${Math.round(used/total*100)}%)`);
            }
        }
        
        // 頁面載入效能
        window.addEventListener('load', () => {
            setTimeout(() => {
                const perfData = performance.timing;
                const pageLoadTime = perfData.loadEventEnd - perfData.navigationStart;
                const connectTime = perfData.responseEnd - perfData.requestStart;
                const renderTime = perfData.domComplete - perfData.domLoading;
                
                console.log('🚀 Performance Metrics:');
                console.log(`   Page Load: ${pageLoadTime}ms`);
                console.log(`   Server Response: ${connectTime}ms`);
                console.log(`   DOM Render: ${renderTime}ms`);
                
                if (pageLoadTime > 3000) {
                    console.warn('⚠️ Slow page load detected!');
                }
                
                logMemoryUsage();
            }, 0);
        });
        
        // 啟動 FPS 監控
        requestAnimationFrame(measureFPS);
        
        // 每 30 秒記錄一次記憶體使用
        setInterval(logMemoryUsage, 30000);
        
        // 監控長時間執行的任務
        const observer = new PerformanceObserver((list) => {
            for (const entry of list.getEntries()) {
                if (entry.duration > 50) {
                    console.warn(`⏱️ Long task detected: ${entry.name} took ${Math.round(entry.duration)}ms`);
                }
            }
        });
        
        try {
            observer.observe({ entryTypes: ['longtask', 'measure'] });
        } catch (e) {
            // longtask API not supported
        }
        
        console.log('✅ Performance monitoring enabled');
        console.log('💡 Tips for better performance:');
        console.log('   1. Keep backdrop-filter usage minimal');
        console.log('   2. Avoid unnecessary animations');
        console.log('   3. Use will-change sparingly');
        console.log('   4. Minimize box-shadow complexity');
    }
})();
