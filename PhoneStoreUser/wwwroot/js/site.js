window.toggleBodyScroll = function (disable) {
    if (disable) {
        document.body.classList.add("overflow-hidden");
    } else {
        document.body.classList.remove("overflow-hidden");
    }
};

(function () {
    var win = window.Window && window.Window.prototype;
    if (!win || typeof win.postMessage !== "function") {
        return;
    }

    var originalPostMessage = win.postMessage;

    win.postMessage = function (message, targetOrigin, transfer) {
        var hasThirdArg = arguments.length > 2;
        if (hasThirdArg && transfer != null) {
            var symbolIterator = typeof Symbol !== "undefined" ? Symbol.iterator : null;
            var isIterable = symbolIterator && typeof transfer[symbolIterator] === "function";
            var hasLengthProp = typeof transfer.length === "number";

            if (!isIterable && !hasLengthProp) {
                // Invalid transfer list provided by third-party scripts; drop it to avoid runtime crashes.
                return originalPostMessage.call(this, message, targetOrigin);
            }
        }

        return hasThirdArg
            ? originalPostMessage.call(this, message, targetOrigin, transfer)
            : originalPostMessage.call(this, message, targetOrigin);
    };
})();

window.phoneStore = window.phoneStore || {};
window.phoneStore.printInvoice = function () {
    window.print();
};

// Download file from base64 string
window.downloadFileFromBase64 = function (base64, fileName, contentType) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType });
    
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(link.href);
};

// Chart.js interop functions
window.chartInstances = window.chartInstances || {};

// Helper for Blazor to check Chart.js readiness without using eval
window.ensureChartReady = function () {
    return typeof window !== 'undefined'
        && typeof Chart !== 'undefined'
        && typeof window.createBarChart === 'function'
        && typeof window.createLineChart === 'function';
};

// Lazy loader for Chart.js (local first, CDN fallback)
(function () {
    let loadingPromise = null;

    function injectScript(src) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.async = true;
            script.onload = () => resolve(true);
            script.onerror = () => reject(new Error(`Failed to load ${src}`));
            document.head.appendChild(script);
        });
    }

    window.loadChartJs = function () {
        if (typeof Chart !== 'undefined') {
            return Promise.resolve(true);
        }

        if (loadingPromise) {
            return loadingPromise;
        }

        const localSrc = '/lib/chartjs/chart.umd.min.js';
        const cdnSrc = 'https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js';

        loadingPromise = injectScript(localSrc)
            .catch(() => injectScript(cdnSrc))
            .catch(() => false)
            .finally(() => { loadingPromise = null; });

        return loadingPromise;
    };
})();

window.createBarChart = function (canvasId, labels, data, label, backgroundColor, borderColor) {
    // Kiểm tra Chart.js đã load chưa
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded yet');
        return;
    }

    // Destroy existing chart if it exists
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
    }

    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas element not found:', canvasId);
        return;
    }

    const ctx = canvas.getContext('2d');
    
    window.chartInstances[canvasId] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: label,
                data: data,
                backgroundColor: backgroundColor || 'rgba(59, 130, 246, 0.5)',
                borderColor: borderColor || 'rgb(59, 130, 246)',
                borderWidth: 1,
                borderRadius: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        font: {
                            family: "'Inter', sans-serif",
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            let value = context.parsed.y;
                            if (value >= 1000000000) {
                                return label + ': ₫' + (value / 1000000000).toFixed(1) + 'B';
                            } else if (value >= 1000000) {
                                return label + ': ₫' + (value / 1000000).toFixed(1) + 'M';
                            } else if (value >= 1000) {
                                return label + ': ₫' + (value / 1000).toFixed(0) + 'K';
                            }
                            return label + ': ₫' + value.toLocaleString('vi-VN');
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            if (value >= 1000000000) {
                                return '₫' + (value / 1000000000).toFixed(1) + 'B';
                            } else if (value >= 1000000) {
                                return '₫' + (value / 1000000).toFixed(1) + 'M';
                            } else if (value >= 1000) {
                                return '₫' + (value / 1000).toFixed(0) + 'K';
                            }
                            return '₫' + value;
                        },
                        font: {
                            family: "'Inter', sans-serif",
                            size: 11
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    ticks: {
                        font: {
                            family: "'Inter', sans-serif",
                            size: 11
                        }
                    },
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
};

window.createLineChart = function (canvasId, labels, data, label, borderColor, backgroundColor) {
    // Kiểm tra Chart.js đã load chưa
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded yet');
        return;
    }

    // Destroy existing chart if it exists
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
    }

    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas element not found:', canvasId);
        return;
    }

    const ctx = canvas.getContext('2d');
    
    window.chartInstances[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: label,
                data: data,
                borderColor: borderColor || 'rgb(16, 185, 129)',
                backgroundColor: backgroundColor || 'rgba(16, 185, 129, 0.1)',
                borderWidth: 2,
                fill: true,
                tension: 0.3,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        font: {
                            family: "'Inter', sans-serif",
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            let value = context.parsed.y;
                            if (value >= 1000000000) {
                                return label + ': ₫' + (value / 1000000000).toFixed(1) + 'B';
                            } else if (value >= 1000000) {
                                return label + ': ₫' + (value / 1000000).toFixed(1) + 'M';
                            } else if (value >= 1000) {
                                return label + ': ₫' + (value / 1000).toFixed(0) + 'K';
                            }
                            return label + ': ₫' + value.toLocaleString('vi-VN');
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            if (value >= 1000000000) {
                                return '₫' + (value / 1000000000).toFixed(1) + 'B';
                            } else if (value >= 1000000) {
                                return '₫' + (value / 1000000).toFixed(1) + 'M';
                            } else if (value >= 1000) {
                                return '₫' + (value / 1000).toFixed(0) + 'K';
                            }
                            return '₫' + value;
                        },
                        font: {
                            family: "'Inter', sans-serif",
                            size: 11
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    ticks: {
                        font: {
                            family: "'Inter', sans-serif",
                            size: 11
                        }
                    },
                    grid: {
                        display: false
                    }
                }
            }
        }
    });
};

window.destroyChart = function (canvasId) {
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
        delete window.chartInstances[canvasId];
    }
};
