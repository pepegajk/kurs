window.renderPieChart = (canvasId, labels, data, title) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart if exists
    if (Chart.getChart(canvasId)) {
        Chart.getChart(canvasId).destroy();
    }

    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                label: title,
                data: data,
                backgroundColor: [
                    'rgba(255, 99, 132, 0.8)',
                    'rgba(54, 162, 235, 0.8)',
                    'rgba(255, 206, 86, 0.8)',
                    'rgba(75, 192, 192, 0.8)',
                    'rgba(153, 102, 255, 0.8)',
                ],
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                title: {
                    display: false
                }
            }
        }
    });
};

window.renderBarChart = (canvasId, labels, data, title) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    if (Chart.getChart(canvasId)) {
        Chart.getChart(canvasId).destroy();
    }

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: title,
                data: data,
                backgroundColor: 'rgba(54, 162, 235, 0.8)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
};

window.renderLineChart = (canvasId, labels, data, title) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    if (Chart.getChart(canvasId)) {
        Chart.getChart(canvasId).destroy();
    }

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: title,
                data: data,
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 3,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    }
                }
            }
        }
    });
};

// Улучшенный график сделок: столбцы для количества и линия для выручки
window.renderDealsChart = function(canvasId, labels, dealsCount, revenue) {
    console.log('renderDealsChart FUNCTION CALLED!', { canvasId, labelsLength: labels?.length, dealsCountLength: dealsCount?.length, revenueLength: revenue?.length });
    // Проверяем, что Chart.js загружен
    if (typeof Chart === 'undefined') {
        console.error('Chart.js is not loaded!');
        setTimeout(() => window.renderDealsChart(canvasId, labels, dealsCount, revenue), 100);
        return;
    }
    
    console.log('renderDealsChart called', { canvasId, labels, dealsCount, revenue });
    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        console.error('Canvas element not found:', canvasId);
        return;
    }

    // Destroy existing chart if exists
    const existingChart = Chart.getChart(canvasId);
    if (existingChart) {
        existingChart.destroy();
    }

    // Форматируем выручку для отображения
    const formatRevenue = (value) => {
        if (value >= 1000000) {
            return (value / 1000000).toFixed(1) + 'М';
        }
        if (value >= 1000) {
            return (value / 1000).toFixed(0) + 'К';
        }
        return value.toFixed(0);
    };

    // Проверяем данные
    if (!labels || !dealsCount || !revenue) {
        console.error('Invalid data provided:', { labels, dealsCount, revenue });
        return;
    }

    if (labels.length === 0 || dealsCount.length === 0) {
        console.error('Empty data arrays');
        return;
    }

    try {
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Количество сделок',
                        data: dealsCount,
                        type: 'bar',
                        backgroundColor: 'rgba(54, 162, 235, 0.7)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 2,
                        yAxisID: 'y',
                        order: 2
                    },
                    {
                        label: 'Выручка (руб.)',
                        data: revenue,
                        type: 'line',
                        borderColor: 'rgba(255, 99, 132, 1)',
                        backgroundColor: 'rgba(255, 99, 132, 0.1)',
                        borderWidth: 3,
                        fill: false,
                        tension: 0.4,
                        pointRadius: 5,
                        pointHoverRadius: 7,
                        pointBackgroundColor: 'rgba(255, 99, 132, 1)',
                        pointBorderColor: '#fff',
                        pointBorderWidth: 2,
                        yAxisID: 'y1',
                        order: 1
                    }
                ]
            },
            options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        font: {
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            if (context.datasetIndex === 0) {
                                label += context.parsed.y + ' шт.';
                            } else {
                                label += new Intl.NumberFormat('ru-RU', {
                                    style: 'currency',
                                    currency: 'RUB',
                                    minimumFractionDigits: 0
                                }).format(context.parsed.y);
                            }
                            return label;
                        }
                    },
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 10,
                    titleFont: {
                        size: 14,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        maxRotation: 45,
                        minRotation: 45,
                        font: {
                            size: 11
                        }
                    }
                },
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Количество сделок',
                        font: {
                            size: 12,
                            weight: 'bold'
                        }
                    },
                    ticks: {
                        precision: 0,
                        font: {
                            size: 11
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Выручка (руб.)',
                        font: {
                            size: 12,
                            weight: 'bold'
                        }
                    },
                    ticks: {
                        callback: function(value) {
                            return formatRevenue(value);
                        },
                        font: {
                            size: 11
                        }
                    },
                    grid: {
                        drawOnChartArea: false
                    }
                }
            }
        }
        });
        console.log('Chart created successfully');
    } catch (error) {
        console.error('Error creating chart:', error);
    }
};

// Проверка после определения функции
console.log('charts.js loaded. renderDealsChart defined:', typeof window.renderDealsChart !== 'undefined');
