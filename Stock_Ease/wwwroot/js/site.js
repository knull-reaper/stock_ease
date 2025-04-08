﻿document.addEventListener('DOMContentLoaded', function () {

    
    const originalNavButtons = document.querySelectorAll('#nav-content .nav-button'); 
    const currentPath = window.location.pathname;
    let defaultActiveSet = false;

    originalNavButtons.forEach((button) => {
        const link = button.querySelector('a');
        if (link) {
            const href = link.getAttribute('href');
            
            if (href === currentPath || (href !== '/' && currentPath.startsWith(href)) || (href === '/Home/Index' && currentPath === '/')) {
                
                button.style.color = 'var(--sidebar-active-text-color, #ffffff)';
                
                const index = Array.from(originalNavButtons).indexOf(button);
                const highlight = document.getElementById('nav-content-highlight');
                if (highlight) {
                    highlight.style.top = `${16 + index * 54}px`; 
                }
                defaultActiveSet = true;
            } else {
                button.style.color = ''; 
            }
        }
    });
    

    
    


    
    function createRipple(event) {
        
        const button = event.currentTarget;
        if (!button.closest('.main-content')) return; 

        const existingRipple = button.querySelector(".ripple");
        if (existingRipple) {
            existingRipple.remove();
        }

        const circle = document.createElement("span");
        const diameter = Math.max(button.clientWidth, button.clientHeight);
        const radius = diameter / 2;

        circle.style.width = circle.style.height = `${diameter}px`;
        
        const rect = button.getBoundingClientRect();
        circle.style.left = `${event.clientX - rect.left - radius}px`;
        circle.style.top = `${event.clientY - rect.top - radius}px`;
        circle.classList.add("ripple");

        button.appendChild(circle);

        
        circle.addEventListener('animationend', () => {
            circle.remove();
        });
    }

    
    const mainContentRippleElements = document.querySelectorAll('.main-content .btn'); 
    mainContentRippleElements.forEach(element => {
        element.addEventListener("click", createRipple);
    });


    
    const toastContainer = document.getElementById('toast-container');

    function showToast(message, type = 'info', duration = 5000) {
        if (!toastContainer) return;

        const toast = document.createElement('div');
        toast.className = `toast-notification ${type}`; 
        toast.textContent = message;

        toastContainer.appendChild(toast);

        
        toast.offsetHeight;

        
        toast.classList.add('show');

        
        setTimeout(() => {
            toast.classList.remove('show');
            
            toast.addEventListener('transitionend', () => {
                if (toast.parentNode === toastContainer) { 
                    toastContainer.removeChild(toast);
                }
            });
        }, duration);
    }

    
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/transactionHub") 
        .configureLogging(signalR.LogLevel.Information)
        .build();

    async function startSignalR() {
        try {
            await connection.start();
            console.log("SignalR Connected.");
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            
            setTimeout(startSignalR, 5000);
        }
    }

    connection.onclose(async () => {
        console.log("SignalR connection closed. Attempting to restart...");
        await startSignalR();
    });

    

    
    connection.on("ReceiveAlertNotification", (message) => {
        console.log("Alert Received:", message);
        showToast(message, 'warning'); 
    });

    
    connection.on("ReceiveTransactionUpdate", (data) => {
        console.log("Transaction Update Received:", data);
        
        
        
        
        showToast(`Transaction ${data.transaction.transactionId} updated. Product: ${data.product.name} Qty: ${data.product.quantity}`, 'success');
        
        if (window.location.pathname.includes('/Transactions')) {
             
             
             updateTransactionRow(data.transaction, data.product); 
        }
        updateProductDisplay(data.product); 
    });

    
    connection.on("ReceiveTransactionDeletion", (data) => {
        console.log("Transaction Deletion Received:", data);
        
        
        
        showToast(`Transaction ${data.transactionId} deleted. Product: ${data.product?.name} Qty: ${data.product?.quantity}`, 'info');
         
        if (window.location.pathname.includes('/Transactions')) {
            
            removeTransactionRow(data.transactionId); 
        }
        if (data.product) {
            updateProductDisplay(data.product); 
        }
    });

    
    startSignalR();


    
    const alertCheckInterval = 60000; 

    async function fetchUnreadAlerts() {
        try {
            const response = await fetch('/api/alerts/unread');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const alerts = await response.json();
            if (alerts && alerts.length > 0) {
                alerts.forEach(alert => {
                    console.log("Polled Alert:", alert);
                    
                    
                    if (!document.querySelector('.toast-notification.warning')) {
                         showToast(alert.message, 'warning');
                    }
                });
            }
        } catch (error) {
            console.error("Error fetching alerts:", error);
        }
    }

    
    
    


    
    
    
    const createForm = document.getElementById('createTransactionForm'); 

    if (createForm) {
        createForm.addEventListener('submit', async function (event) {
            event.preventDefault(); 

            const formData = new FormData(createForm);
            const url = createForm.action;
            const method = createForm.method;

            
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) {
                 
                 
                 
                 
            }

            try {
                const response = await fetch(url, {
                    method: method,
                    body: new URLSearchParams(formData), 
                    headers: {
                        
                         'RequestVerificationToken': token ? token.value : '',
                        'X-Requested-With': 'XMLHttpRequest' 
                    }
                });

                
                console.log('AJAX Response Status:', response.status);
                console.log('AJAX Response Headers:', response.headers);

                const result = await response.json(); 
                console.log('AJAX Received Result:', result); 

                if (result.success) {
                    console.log('AJAX Handler: Success branch entered.');
                    
                    
                    createForm.reset(); 
                    
                    
                } else {
                    
                    let errorMessage = result.message || 'An error occurred.';
                    if (result.errors && result.errors.length > 0) {
                        errorMessage += '\n- ' + result.errors.join('\n- ');
                    }
                    showToast(errorMessage, 'error');
                    console.log('AJAX Handler: Error branch processed.');
                }
            } catch (error) {
                console.error('AJAX Form submission fetch/parse error:', error); 
                showToast('An error occurred while submitting the form or processing the response.', 'error');
            }
        });
    }

    
    function updateTransactionRow(transaction, product) {
        
        const tableBody = document.getElementById('transactionTableBody');
        if (!tableBody) return;

        let row = tableBody.querySelector(`tr[data-transaction-id="${transaction.transactionId}"]`);

        
        const formattedDate = new Date(transaction.transactionDate).toLocaleString();

        if (row) { 
            row.cells[1].textContent = transaction.userId; 
            row.cells[2].textContent = product.name; 
            row.cells[3].textContent = transaction.quantity; 
            row.cells[4].textContent = formattedDate; 
            
        } else { 
            row = tableBody.insertRow(0); 
            row.setAttribute('data-transaction-id', transaction.transactionId);
            row.innerHTML = `
                <td>${transaction.transactionId}</td>
                <td>${transaction.userId}</td>
                <td>${product.name}</td>
                <td>${transaction.quantity}</td>
                <td>${formattedDate}</td>
                <td>
                    <a href="/Transactions/Edit/${transaction.transactionId}">Edit</a> |
                    <a href="/Transactions/Details/${transaction.transactionId}">Details</a> |
                    <a href="/Transactions/Delete/${transaction.transactionId}">Delete</a>
                    
                </td>
            `;
        }
        
        row.classList.add('highlight');
        setTimeout(() => row.classList.remove('highlight'), 2000);
    }

    function removeTransactionRow(transactionId) {
        const tableBody = document.getElementById('transactionTableBody');
        if (!tableBody) return;
        const row = tableBody.querySelector(`tr[data-transaction-id="${transactionId}"]`);
        if (row) {
            row.remove();
        }
    }

    function updateProductDisplay(product) {
        
        const quantityElement = document.querySelector(`.product-quantity[data-product-id="${product.productId}"]`);
        if (quantityElement) {
            quantityElement.textContent = product.quantity;
            
            if (product.quantity <= LowStockThreshold) { 
                 quantityElement.classList.add('low-stock-warning');
            } else {
                 quantityElement.classList.remove('low-stock-warning');
            }
        }
    }

    
    
    
    
    
    
    


});
