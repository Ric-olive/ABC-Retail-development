// Home page functionality - Search and Filter
document.addEventListener('DOMContentLoaded', function() {
    // Search functionality
    window.performSearch = function() {
        const searchTerm = document.getElementById('searchInput').value.toLowerCase().trim();
        const productItems = document.querySelectorAll('.product-item');
        
        if (searchTerm === '') {
            productItems.forEach(item => {
                item.style.display = 'block';
            });
            return;
        }

        productItems.forEach(item => {
            const productTitle = item.querySelector('.product-title').textContent.toLowerCase();
            const productCategory = item.dataset.category.toLowerCase();
            
            if (productTitle.includes(searchTerm) || productCategory.includes(searchTerm)) {
                item.style.display = 'block';
            } else {
                item.style.display = 'none';
            }
        });

        const visibleItems = document.querySelectorAll('.product-item[style*="block"]').length;
        showSearchResults(visibleItems, searchTerm);
    };

    // Category filter functionality
    window.filterByCategory = function(category = null) {
        const selectedCategory = category || document.getElementById('categoryFilter').value;
        const productItems = document.querySelectorAll('.product-item');
        
        productItems.forEach(item => {
            if (selectedCategory === '' || item.dataset.category === selectedCategory) {
                item.style.display = 'block';
            } else {
                item.style.display = 'none';
            }
        });

        if (category) {
            document.getElementById('categoryFilter').value = category;
        }

        if (selectedCategory !== '') {
            document.getElementById('searchInput').value = '';
        }
    };

    // Search on Enter key
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                performSearch();
            }
        });

        // Real-time search suggestions
        searchInput.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase().trim();
            const suggestions = document.getElementById('searchSuggestions');
            
            if (searchTerm.length >= 2) {
                const productTitles = Array.from(document.querySelectorAll('.product-title'))
                    .map(el => el.textContent)
                    .filter(title => title.toLowerCase().includes(searchTerm))
                    .slice(0, 5);
                
                if (productTitles.length > 0) {
                    suggestions.innerHTML = productTitles
                        .map(title => `<div class="suggestion-item p-2 border-bottom" style="cursor: pointer;" onclick="selectSuggestion('${title}')">${title}</div>`)
                        .join('');
                    suggestions.style.display = 'block';
                } else {
                    suggestions.style.display = 'none';
                }
            } else {
                suggestions.style.display = 'none';
            }
        });
    }

    window.selectSuggestion = function(suggestion) {
        document.getElementById('searchInput').value = suggestion;
        document.getElementById('searchSuggestions').style.display = 'none';
        performSearch();
    };

    // Hide suggestions when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.search-container')) {
            const suggestions = document.getElementById('searchSuggestions');
            if (suggestions) {
                suggestions.style.display = 'none';
            }
        }
    });

    function showSearchResults(count, term) {
        const existingAlert = document.querySelector('.search-results-alert');
        if (existingAlert) {
            existingAlert.remove();
        }

        const alert = document.createElement('div');
        alert.className = 'alert alert-info search-results-alert mt-3';
        alert.innerHTML = `<i class="bi bi-info-circle me-2"></i>Found ${count} product${count !== 1 ? 's' : ''} matching "${term}"`;
        
        const container = document.querySelector('.featured-products .container');
        const targetElement = document.querySelector('.featured-products .d-flex');
        if (container && targetElement) {
            container.insertBefore(alert, targetElement);
            
            setTimeout(() => {
                if (alert.parentNode) {
                    alert.remove();
                }
            }, 5000);
        }
    }

    // Enhanced Add to Cart with AJAX
    document.querySelectorAll('.add-to-cart-form').forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const button = this.querySelector('.add-to-cart-btn');
            const originalContent = button.innerHTML;
            
            button.innerHTML = '<i class="bi bi-arrow-repeat"></i> Adding...';
            button.disabled = true;
            
            fetch(this.action, {
                method: 'POST',
                body: new FormData(this)
            })
            .then(response => response.json())
            .then(data => {
                button.disabled = false;
                
                if (data.success) {
                    button.innerHTML = '<i class="bi bi-check-circle me-2"></i>Added!';
                    button.classList.remove('btn-primary');
                    button.classList.add('btn-success');
                    
                    if (typeof updateCartBadge === 'function') {
                        updateCartBadge(data.cartCount);
                    }
                    
                    if (typeof showToast === 'function') {
                        showToast('success', data.message || 'Product added to cart successfully!');
                    }
                    
                    setTimeout(() => {
                        button.innerHTML = originalContent;
                        button.classList.remove('btn-success');
                        button.classList.add('btn-primary');
                    }, 2000);
                } else {
                    button.innerHTML = originalContent;
                    if (typeof showToast === 'function') {
                        showToast('error', data.message || 'Failed to add product to cart');
                    }
                }
            })
            .catch(error => {
                console.error('Error:', error);
                button.disabled = false;
                button.innerHTML = originalContent;
                if (typeof showToast === 'function') {
                    showToast('error', 'An error occurred. Please try again.');
                }
            });
        });
    });
});
