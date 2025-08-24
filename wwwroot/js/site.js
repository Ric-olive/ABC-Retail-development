// Global cart and notification functions
document.addEventListener('DOMContentLoaded', function() {
    // Initialize cart count on page load
    updateCartBadgeFromSession();
});

// Update cart badge with animation
function updateCartBadge(count) {
    const cartBadge = document.querySelector('.cart-badge');
    if (cartBadge) {
        cartBadge.textContent = count;
        cartBadge.style.transform = 'scale(1.3)';
        setTimeout(() => {
            cartBadge.style.transform = 'scale(1)';
        }, 200);
    }
}

// Get cart count from session and update badge
function updateCartBadgeFromSession() {
    fetch('/CustomerCart/GetCartCount')
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateCartBadge(data.cartCount);
            }
        })
        .catch(error => console.log('Cart count fetch failed:', error));
}

// Toast notification system
function showToast(type, message) {
    // Remove existing toasts
    const existingToasts = document.querySelectorAll('.toast-notification');
    existingToasts.forEach(toast => toast.remove());

    const toast = document.createElement('div');
    toast.className = `toast-notification alert alert-${type === 'success' ? 'success' : 'danger'} position-fixed`;
    toast.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        animation: slideInRight 0.3s ease-out;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        border-radius: 8px;
    `;
    
    toast.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-${type === 'success' ? 'check-circle-fill' : 'exclamation-triangle-fill'} me-2"></i>
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" onclick="this.parentElement.parentElement.remove()"></button>
        </div>
    `;

    document.body.appendChild(toast);

    // Auto remove after 4 seconds
    setTimeout(() => {
        if (toast.parentNode) {
            toast.style.animation = 'slideOutRight 0.3s ease-in';
            setTimeout(() => toast.remove(), 300);
        }
    }, 4000);
}

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    @keyframes slideOutRight {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100%); opacity: 0; }
    }
    .cart-badge {
        transition: transform 0.2s ease;
    }
`;
document.head.appendChild(style);
