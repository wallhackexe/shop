/* ==================================================================
   ГЛОБАЛЬНЫЕ ФУНКЦИИ (Вызываются прямо из HTML через onclick)
   ================================================================== */
// Функция для получения токена (защита от ошибок, если токена нет на странице)
const getCsrfToken = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;

// 1. Переключение табов в админке
window.openTab = function (evt, tabName) {
    const tabContent = document.getElementsByClassName("tab-content");
    for (let i = 0; i < tabContent.length; i++) tabContent[i].style.display = "none";

    const tabBtns = document.getElementsByClassName("tab-btn");
    for (let i = 0; i < tabBtns.length; i++) tabBtns[i].classList.remove("active");

    document.getElementById(tabName).style.display = "block";
    evt.currentTarget.classList.add("active");
    evt.currentTarget.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
};

// 2. Копирование в буфер обмена
window.copyToClipboard = function (text) {
    navigator.clipboard.writeText(text).then(() => {
        if (typeof Swal !== 'undefined') {
            Swal.fire({ icon: 'success', title: 'Скопировано!', timer: 1500, showConfirmButton: false });
        } else {
            alert('Ссылка скопирована!');
        }
    });
};

// 3. Управление слайдером на странице деталей товара (Details.cshtml)
let dPos = 0;
window.moveDetailsSlide = function (n) {
    const dStrip = document.getElementById('detailsStrip');
    if (!dStrip) return;
    const dCount = dStrip.querySelectorAll('img').length;
    dPos = (dPos + n + dCount) % dCount;
    window.renderDetailsSlider();
};

window.setDetailsSlide = function (n) {
    dPos = n;
    window.renderDetailsSlider();
};

window.renderDetailsSlider = function () {
    const dStrip = document.getElementById('detailsStrip');
    if (!dStrip) return;
    dStrip.style.transform = `translateX(-${dPos * 100}%)`;
    document.querySelectorAll('.thumb-item').forEach((t, i) => {
        t.classList.toggle('active', i === dPos);
    });
    window.resetDZoom();
};

window.resetDZoom = function () {
    const dStrip = document.getElementById('detailsStrip');
    if (!dStrip) return;
    dStrip.querySelectorAll('img').forEach(i => {
        i.style.transform = "scale(1)";
        i.style.transformOrigin = "center center";
    });
};

window.toggleMobileFilters = function () {
    const sidebar = document.getElementById('filterSidebar');
    if (sidebar) {
        sidebar.classList.toggle('active');

        if (sidebar.classList.contains('active')) {
            document.body.style.overflow = 'hidden';
        } else {
            document.body.style.overflow = '';
        }
    }
};
/* ==================================================================
   ГЛОБАЛЬНЫЕ ФУНКЦИИ (Дополнения)
   ================================================================== */

// 5. Переключение лайка (Избранное)
/* ==================================================================
   ГЛОБАЛЬНЫЕ ФУНКЦИИ: Избранное (toggleLike)
   ================================================================== */
window.toggleLike = function (productId, btn) {
    if (window.event) window.event.stopPropagation();

    const isCurrentlyActive = btn.classList.contains('active');
    const icon = btn.querySelector('i');

    const setIconActive = (isActive) => {
        btn.classList.toggle('active', isActive);
        if (isActive) {
            icon.classList.remove('fa-regular');
            icon.classList.add('fa-solid');
        } else {
            icon.classList.remove('fa-solid');
            icon.classList.add('fa-regular');
        }
    };

    setIconActive(!isCurrentlyActive);

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const formData = new FormData();
    formData.append('productId', productId);

    fetch('/Account/ToggleFavorite', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token,
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: formData 
    })
        .then(res => {
            if (res.status === 401) {
                setIconActive(isCurrentlyActive);

                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        title: 'НУЖНА АВТОРИЗАЦИЯ',
                        html: 'Чтобы сохранять образы в избранное,<br>войдите в аккаунт <b>VESNA</b>',
                        icon: 'info',
                        iconColor: '#DCAEAE',
                        showCancelButton: true,
                        confirmButtonText: 'ВОЙТИ',
                        cancelButtonText: 'ОТМЕНА',
                        buttonsStyling: false,
                        customClass: {
                            popup: 'vesna-popup',
                            title: 'vesna-title',
                            htmlContainer: 'vesna-html',
                            confirmButton: 'vesna-confirm-btn',
                            cancelButton: 'vesna-cancel-btn'
                        }
                    }).then((result) => {
                        if (result.isConfirmed) window.location.href = '/Account/Login';
                    });
                }
                return;
            }

            if (!res.ok) throw new Error('Ошибка сервера');
            return res.json();
        })
        .catch(err => {
            console.error('Ошибка AJAX лайка:', err);
            setIconActive(isCurrentlyActive);
        });
};
// 6. Переключение табов в Личном Кабинете
window.switchProfileTab = function (tabName, btn) {
    const contents = document.querySelectorAll('.tab-content-item');
    contents.forEach(c => c.style.display = 'none');

    const buttons = document.querySelectorAll('.profile-tab-btn');
    buttons.forEach(b => b.classList.remove('active'));

    const target = document.getElementById('tab-' + tabName);
    if (target) {
        target.style.display = 'block';
        btn.classList.add('active');
    }

    if (window.innerWidth <= 992) {
        setTimeout(() => {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 100);
    }
};
/* ==================================================================
   ОСНОВНОЙ БЛОК: ИНИЦИАЛИЗАЦИЯ ПОСЛЕ ЗАГРУЗКИ СТРАНИЦЫ
   ================================================================== */
document.addEventListener('DOMContentLoaded', async () => {
    const isAuthenticated = window.isAuthenticated || false;

    // --- 1. АДМИНКА: БОКОВОЕ МЕНЮ ---
    const burger = document.getElementById('burger');
    const sidebar = document.getElementById('sidebar');
    const adminOverlay = document.getElementById('overlay');

    if (burger && sidebar && adminOverlay) {
        const toggleMenu = () => {
            sidebar.classList.toggle('show');
            adminOverlay.classList.toggle('show');
        };
        burger.addEventListener('click', toggleMenu);
        adminOverlay.addEventListener('click', toggleMenu);
    }

    // --- 2. АДМИНКА: ЗАГРУЗКА ИЗОБРАЖЕНИЙ (ImgBB) ---
    const uploader = document.getElementById('cloudUploader');
    if (uploader) {
        uploader.addEventListener('change', async (e) => {
            const files = e.target.files;
            if (!files.length) return;

            const statusText = document.getElementById('uploadStatus');
            const previewBlock = document.getElementById('previewBlock');
            const imageUrlInput = document.getElementById('imgInput');

            if (statusText) {
                statusText.style.color = '#e67e22';
                statusText.innerText = `Загрузка: 0 из ${files.length}...`;
            }

            let successCount = 0;

            for (let file of files) {
                const formData = new FormData();
                formData.append('image', file);

                try {
                    const response = await fetch('/Admin/ProxyUpload', {
                        method: 'POST',
                        headers: { 'RequestVerificationToken': getCsrfToken() },
                        body: formData
                    });
                    const data = await response.json();

                    if (data.success) {
                        successCount++;
                        if (statusText) statusText.innerText = `Загрузка: ${successCount} из ${files.length}...`;

                        if (imageUrlInput) {
                            imageUrlInput.value = data.data.url;
                            if (previewBlock) {
                                previewBlock.innerHTML = `<img src="${data.data.url}" style="max-width: 100%; max-height: 100%; object-fit: contain; border-radius: 12px;" />`;
                            }
                        }
                        else {
                            await fetch('/Admin/SaveMediaUrl', {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json',
                                    'RequestVerificationToken': getCsrfToken()
                                },
                                body: JSON.stringify(data.data.url)
                            });
                        }
                    }
                } catch (error) {
                    console.error('Ошибка загрузки ImgBB:', error);
                    if (statusText) {
                        statusText.innerText = 'Ошибка сети при загрузке';
                        statusText.style.color = 'red';
                    }
                }
            }

            if (statusText) {
                statusText.style.color = '#27ae60';
                statusText.innerText = `Готово! Загружено ${successCount} фото.`;

                if (!imageUrlInput) {
                    setTimeout(() => location.reload(), 1000);
                }
            }
        });
    }

    // --- 3. СЛАЙДЕР ДЕТАЛЕЙ ТОВАРА (ЗУМ) ---
    const zBox = document.getElementById('zoomContainer');
    const dStrip = document.getElementById('detailsStrip');
    if (zBox && dStrip) {
        zBox.addEventListener('mousemove', function (e) {
            const cur = dStrip.querySelectorAll('img')[dPos];
            const r = zBox.getBoundingClientRect();
            const x = ((e.clientX - r.left) / r.width) * 100;
            const y = ((e.clientY - r.top) / r.height) * 100;
            cur.style.transformOrigin = `${x}% ${y}%`;
            cur.style.transform = "scale(2.5)";
        });
        zBox.addEventListener('mouseleave', window.resetDZoom);
    }

    // --- 4. СИНХРОНИЗАЦИЯ И СЧЕТЧИК КОРЗИНЫ ---
    const cartBadge = document.getElementById('cart-count');
    const guestCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');

    // --- 9. СИСТЕМА РЕЙТИНГА (Звезды в отзывах) ---
    const ratingContainers = document.querySelectorAll('.star-rating-input');

    ratingContainers.forEach(container => {
        const stars = container.querySelectorAll('i');
        const input = container.querySelector('input');

        stars.forEach(star => {
            star.addEventListener('mouseover', function () {
                const val = this.dataset.value;
                highlightStars(stars, val);
            });

            star.addEventListener('mouseleave', function () {
                highlightStars(stars, input.value);
            });

            star.addEventListener('click', function () {
                input.value = this.dataset.value;
                highlightStars(stars, input.value);
            });
        });
    });

    function highlightStars(stars, count) {
        stars.forEach(s => {
            if (s.dataset.value <= count) {
                s.classList.replace('fa-regular', 'fa-solid');
                s.style.color = '#FFD700';
            } else {
                s.classList.replace('fa-solid', 'fa-regular');
                s.style.color = '#ccc';
            }
        });
    }

    // --- 10. ОТПРАВКА ОТЗЫВА (AJAX) ---
    window.submitReview = async function (productId) {
        const form = document.getElementById(`review-form-${productId}`);
        if (!form) return;

        const rating = form.querySelector('input[name="Rating"]').value;
        const text = form.querySelector('textarea').value;

        if (!text.trim()) {
            Swal.fire('Ошибка', 'Напишите хотя бы пару слов в отзыве', 'error');
            return;
        }

        const response = await fetch('/Account/AddReview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify({ productId, rating, text })
        });

        if (response.ok) {
            Swal.fire('Спасибо!', 'Ваш отзыв опубликован', 'success')
                .then(() => location.reload());
        }
    }
    // 1. Показываем гостевое количество при загрузке
    if (!isAuthenticated && cartBadge && guestCart.length > 0) {
        cartBadge.textContent = guestCart.length;
        cartBadge.style.display = 'block';
    }

    // 2. Синхронизация при входе
    if (isAuthenticated && guestCart.length > 0) {
        fetch('/Account/SyncCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getCsrfToken()
            },
            body: JSON.stringify(guestCart)
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    localStorage.removeItem('guest_cart');

                    updateCartVisuals(data.cartCount);

                    if (window.location.pathname.toLowerCase().includes('/cart')) {
                        location.reload();
                    }
                }
            })
            .catch(err => console.error("Ошибка синхронизации:", err));
    }

    function updateCartVisuals(exactCount = null) {
        if (!cartBadge) return;

        let finalCount = 0;

        if (exactCount !== null && exactCount !== undefined) {
            finalCount = exactCount;
        }
        else if (!isAuthenticated) {
            const currentGuestCart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
            finalCount = currentGuestCart.length;
        }
        else {
            finalCount = parseInt(cartBadge.textContent) || 0;
        }

        cartBadge.textContent = finalCount;
        cartBadge.style.display = finalCount > 0 ? 'block' : 'none';

        cartBadge.classList.remove('pulse');
        void cartBadge.offsetWidth;
        cartBadge.classList.add('pulse');
    }

    // --- 5. УМНАЯ ЛОГИКА ДОБАВЛЕНИЯ В КОРЗИНУ (С +/- И АНИМАЦИЕЙ) ---
    async function handleCartAction(url, productId, isAdd) {
        if (!isAuthenticated) {
            let cart = JSON.parse(localStorage.getItem('guest_cart') || '[]');
            if (isAdd) {
                let id = parseInt(productId);
                if (!cart.includes(id)) {
                    cart.push(id);
                    localStorage.setItem('guest_cart', JSON.stringify(cart));
                } else {
                    return { success: false, message: "Товар уже в корзине. Войдите, чтобы менять количество." };
                }
            }
            return { success: true, guest: true, cartCount: cart.length };
        }

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': getCsrfToken() }
            });
            return await response.json();
        } catch (err) {
            console.error("Ошибка корзины:", err);
            return { success: false, message: "Ошибка соединения с сервером" };
        }
    }

    document.addEventListener('click', async (e) => {
        const addBtn = e.target.closest('.initial-add-btn') || e.target.closest('.btn-add-cart');

        // А) Клик по "В корзину" или начальному "+"
        if (addBtn && !addBtn.classList.contains('qty-btn')) {
            e.preventDefault(); e.stopPropagation();
            if (addBtn.classList.contains('already-added')) return;

            let productId = addBtn.dataset.id;
            const wrapper = addBtn.closest('.cart-controls-wrapper');
            if (wrapper) productId = wrapper.dataset.productId;
            if (!productId) {
                const card = addBtn.closest('.product-card');
                if (card) productId = card.dataset.id;
            }

            if (!productId) return;

            const data = await handleCartAction(`/Cart/Add?productId=${productId}`, productId, true);

            if (data.success || data.guest) {
                updateCartVisuals(data.cartCount);
                if (wrapper) {
                    addBtn.style.display = 'none';
                    let qtyControls = wrapper.querySelector('.qty-controls');
                    if (qtyControls) qtyControls.style.display = 'flex';
                    let qtyVal = wrapper.querySelector('.qty-val');
                    if (qtyVal) qtyVal.innerText = '1';
                } else {
                    addBtn.style.background = '#DCAEAE';
                    addBtn.style.color = '#fff';
                    addBtn.classList.add('already-added');
                    const icon = addBtn.querySelector('i');
                    if (icon) icon.className = 'fa-solid fa-check';
                    setTimeout(() => {
                        addBtn.style.background = 'none';
                        addBtn.style.color = '#111';
                    }, 500);
                }
            } else {
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'info',
                        title: 'Упс!',
                        text: data.message,
                        confirmButtonColor: '#111',
                        confirmButtonText: 'Понятно'
                    });
                }
            }
        }

        // Б) Клик по "Плюс" в регуляторе количества
        const plusBtn = e.target.closest('.qty-btn.plus');
        if (plusBtn) {
            e.preventDefault(); e.stopPropagation();
            const wrapper = plusBtn.closest('.cart-controls-wrapper');
            const productId = wrapper.dataset.productId;
            const qtySpan = wrapper.querySelector('.qty-val');

            const data = await handleCartAction(`/Cart/Add?productId=${productId}`, productId, true);

            if (data.success || data.guest) {
                qtySpan.innerText = (parseInt(qtySpan.innerText) || 1) + 1;
                updateCartVisuals(data.cartCount);
            } else {
                // УВЕДОМЛЕНИЕ: Если на складе больше нет
                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Достигнут лимит',
                        text: data.message || "Больше этого товара на складе нет",
                        toast: true,
                        position: 'top-end',
                        showConfirmButton: false,
                        timer: 3000,
                        timerProgressBar: true
                    });
                }
            }
        }

        // В) Клик по "Минус"
        const minusBtn = e.target.closest('.qty-btn.minus');
        if (minusBtn) {
            e.preventDefault(); e.stopPropagation();
            const wrapper = minusBtn.closest('.cart-controls-wrapper');
            const productId = wrapper.dataset.productId;
            const qtySpan = wrapper.querySelector('.qty-val');

            const data = await handleCartAction(`/Cart/DecreaseOne?productId=${productId}`, productId, false);
            if (data.success || data.guest) {
                let current = parseInt(qtySpan.innerText) || 1;
                if (current > 1) {
                    qtySpan.innerText = current - 1;
                } else {
                    wrapper.querySelector('.qty-controls').style.display = 'none';
                    wrapper.querySelector('.initial-add-btn').style.display = 'flex';
                }
                updateCartVisuals(data.cartCount);
            }
        }
    });

    // --- 5.1 КНОПКА ДОБАВЛЕНИЯ В КАРТОЧКЕ ТОВАРА (Details.cshtml) ---
    const detailsBtn = document.getElementById('details-add-btn');
    if (detailsBtn) {
        detailsBtn.addEventListener('click', async function (e) {
            e.preventDefault();

            this.disabled = true;
            const productId = this.getAttribute('data-id');
            const originalText = this.innerText;

            const data = await handleCartAction(`/Cart/Add?productId=${productId}`, productId, true);

            if (data.success || data.guest) {
                updateCartVisuals(data.cartCount);

                this.style.background = '#27ae60';
                this.style.borderColor = '#27ae60';
                this.innerHTML = '<i class="fa-solid fa-check"></i> ДОБАВЛЕНО';

                setTimeout(() => {
                    this.style.background = '#111';
                    this.style.borderColor = '#111';
                    this.innerText = originalText;
                    this.disabled = false;
                }, 1500);
            } else {

                if (typeof Swal !== 'undefined') {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Упс!',
                        text: data.message,
                        confirmButtonColor: '#111'
                    });
                }
                this.disabled = false;
            }
        });
    }
    // --- 6. БЫСТРЫЙ ПРОСМОТР (Главная) ИЛИ ПЕРЕХОД (Каталог) ---
    let currentSlide = 0;
    let totalSlides = 0;

    function updateModalSlider() {
        const container = document.getElementById('v2-slides');
        const dotsContainer = document.getElementById('v2-dots');
        if (container && dotsContainer) {
            container.style.transform = `translateX(-${currentSlide * 100}%)`;
            dotsContainer.querySelectorAll('.dot').forEach((dot, index) => {
                dot.style.background = index === currentSlide ? '#111' : '#ccc';
            });
        }
    }

    document.addEventListener('click', (e) => {
        const card = e.target.closest('.product-card');

        if (card && !e.target.closest('.cart-controls-wrapper') && !e.target.closest('.btn-add-cart')) {
            const modal = document.getElementById('v2-modal');
            const id = card.dataset.id;

            if (modal) {

                const isQuickViewClick = e.target.closest('.product-image-box') || e.target.closest('.btn-quick-view-v2') || e.target.closest('.btn-quick-view');

                if (isQuickViewClick) {
                    e.preventDefault();
                    try {
                        document.getElementById('v2-title').innerText = card.dataset.name || "";
                        document.getElementById('v2-price').innerText = parseFloat((card.dataset.price || "0").replace(',', '.')).toLocaleString() + ' ₽';
                        document.getElementById('v2-desc').innerText = card.dataset.desc || "Описание скоро появится.";
                        document.getElementById('v2-cat').innerText = (card.dataset.cat || "").toUpperCase();

                        const modalCartBtn = document.querySelector('.modal-add-to-cart-btn');
                        if (modalCartBtn) {
                            modalCartBtn.dataset.id = id;
                        }

                        const linkEl = document.getElementById('v2-link');
                        if (linkEl) linkEl.href = `/Home/Details/${id}`;

                        const mainImg = card.querySelector('img') ? card.querySelector('img').src : "";
                        const addImgs = card.dataset.additional || "";
                        const allImages = [mainImg, ...addImgs.split(/[\n\r,]+/).filter(url => url.trim() !== "")];
                        allImages.pop();

                        const container = document.getElementById('v2-slides');
                        const dotsContainer = document.getElementById('v2-dots');
                        const prevBtn = document.getElementById('v2-prev');
                        const nextBtn = document.getElementById('v2-next');

                        if (container && dotsContainer) {
                            container.innerHTML = ''; dotsContainer.innerHTML = '';
                            allImages.forEach((url, idx) => {
                                container.insertAdjacentHTML('beforeend', `<img src="${url}" style="width:100%; height:100%; object-fit:cover; flex-shrink:0;">`);
                                dotsContainer.insertAdjacentHTML('beforeend', `<div class="dot" style="width:8px; height:8px; border-radius:50%; background:#ccc; cursor:pointer;"></div>`);
                            });
                            drawModalLines(card);
                        }

                        currentSlide = 0; totalSlides = allImages.length;
                        if (prevBtn) prevBtn.style.display = totalSlides > 1 ? 'block' : 'none';
                        if (nextBtn) nextBtn.style.display = totalSlides > 1 ? 'block' : 'none';
                        updateModalSlider();

                        modal.classList.add('active');
                        document.body.style.overflow = 'hidden';
                    } catch (err) {
                        console.error("Ошибка при открытии предпросмотра:", err);
                        document.body.style.overflow = '';
                    }
                }
            }
            else {
                e.preventDefault();
                if (id) {
                    window.location.href = `/Home/Details/${id}`;
                }
            }
        }
    });

    const prevBtn = document.getElementById('v2-prev');
    const nextBtn = document.getElementById('v2-next');
    if (nextBtn) nextBtn.onclick = (e) => { e.stopPropagation(); if (totalSlides > 0) { currentSlide = (currentSlide + 1) % totalSlides; updateModalSlider(); } };
    if (prevBtn) prevBtn.onclick = (e) => { e.stopPropagation(); if (totalSlides > 0) { currentSlide = (currentSlide - 1 + totalSlides) % totalSlides; updateModalSlider(); } };

    const closeModal = () => { const modal = document.getElementById('v2-modal'); if (modal) { modal.classList.remove('active'); document.body.style.overflow = ''; } };
    const closeBtn = document.getElementById('v2-close');
    if (closeBtn) closeBtn.onclick = closeModal;

    document.addEventListener('click', (e) => {
        const modal = document.getElementById('v2-modal');
        if (modal && e.target === modal) closeModal();
    });

    document.addEventListener('click', (e) => {
        const modalBtn = e.target.closest('.modal-add-to-cart-btn');
        if (modalBtn) {
            e.preventDefault();
            const id = modalBtn.dataset.id;

            const originalCardBtn = document.querySelector(`.product-card[data-id="${id}"] .btn-add-cart`);

            if (originalCardBtn) {
                originalCardBtn.click();
            }

            modalBtn.style.backgroundColor = '#27ae60';
            modalBtn.style.color = '#fff';
            modalBtn.style.pointerEvents = 'none';

            const isAuth = document.querySelector('a[href="/Account/Logout"]');

            if (isAuth) {
                modalBtn.innerText = 'В КОРЗИНЕ (перейдите в корзину чтобы менять количество)';
            } else {
                modalBtn.innerText = 'В КОРЗИНЕ (зайдите в профиль чтобы менять количество)';
            }
        }
    });
    // ----------------------------------------------------------

    document.addEventListener('keydown', (e) => { if (e.key === 'Escape') closeModal(); });

    // --- 7. МОБИЛЬНОЕ МЕНЮ И ЧИПСЫ ---
    const menuOpen = document.getElementById('menu-open-btn');
    const menuClose = document.getElementById('menu-close-btn');
    const mobileMenu = document.getElementById('mobile-menu');

    if (menuOpen && mobileMenu) menuOpen.onclick = () => { mobileMenu.classList.add('active'); document.body.style.overflow = 'hidden'; };
    if (menuClose && mobileMenu) menuClose.onclick = () => { mobileMenu.classList.remove('active'); document.body.style.overflow = ''; };
    document.querySelectorAll('.mobile-nav-links a').forEach(link => {
        link.onclick = () => { mobileMenu.classList.remove('active'); document.body.style.overflow = ''; };
    });

    document.addEventListener('click', (e) => {
        if (e.target.classList.contains('chip')) {
            e.target.parentElement.querySelectorAll('.chip').forEach(c => c.classList.remove('active'));
            e.target.classList.add('active');
        }
    });

    // --- 8. АВТОРИЗАЦИЯ И COOKIE ---
    const cookieBanner = document.getElementById('cookie-consent-banner');
    const acceptCookiesBtn = document.getElementById('accept-cookies-btn');
    if (cookieBanner && !localStorage.getItem('cookieConsentAccepted')) cookieBanner.style.display = 'block';
    if (acceptCookiesBtn) acceptCookiesBtn.onclick = () => { localStorage.setItem('cookieConsentAccepted', 'true'); cookieBanner.style.display = 'none'; };
});

window.toggleEdit = function (formId) {
    const form = document.getElementById(formId);
    const allForms = document.querySelectorAll('.s-edit-form');

    allForms.forEach(f => {
        if (f.id !== formId) f.style.display = 'none';
    });

    form.style.display = form.style.display === 'block' ? 'none' : 'block';
};

window.saveName = function () {
    const name = document.getElementById('input-name').value;
    fetch('/User/UpdateName?fullName=' + encodeURIComponent(name), { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                document.getElementById('display-name').innerText = name;
                toggleEdit('name-form');
                Swal.fire('Готово!', data.message, 'success');
            }
        });
};

// Смена пароля
window.savePassword = function () {
    const oldP = document.getElementById('old-pass').value;
    const newP = document.getElementById('new-pass').value;
    const confP = document.getElementById('confirm-pass').value;

    fetch(`/User/ChangePassword?oldPass=${oldP}&newPass=${newP}&confirmPass=${confP}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                Swal.fire('Успех', 'Пароль изменен', 'success');
                toggleEdit('pass-form');
            } else {
                Swal.fire('Ошибка', data.message, 'error');
            }
        });
};

window.saveEmail = function () {
    const email = document.getElementById('input-email').value;
    const code = document.getElementById('input-email-code').value;

    fetch(`/User/ChangeEmail?newEmail=${email}&code=${code}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                document.getElementById('display-email').innerText = email;
                toggleEdit('email-form');
                Swal.fire('Успех', 'Email изменен', 'success');
            } else {
                Swal.fire('Ошибка', data.message, 'error');
            }
        });
};
// 1. Обработка выбора товара из заказа
window.handleReviewSelection = function (products) {
    if (products.length === 1) {
        openReviewModal(products[0].id, products[0].title);
    } else {
        let productsHtml = '<div class="review-selection-grid">';
        products.forEach(p => {
            productsHtml += `
                <div class="review-sel-card" onclick="Swal.clickConfirm(); openReviewModal(${p.id}, '${p.title.replace(/'/g, "\\'")}')">
                    <img src="${p.img}" />
                    <p>${p.title}</p>
                </div>
            `;
        });
        productsHtml += '</div>';

        Swal.fire({
            title: 'ВЫБЕРИТЕ ТОВАР',
            html: productsHtml,
            showConfirmButton: false,
            showCancelButton: true,
            cancelButtonText: 'ОТМЕНА',
            customClass: {
                popup: 'vesna-popup',
                title: 'vesna-title'
            }
        });
    }
};

function drawModalLines(card) {
    const container = document.getElementById('v2-slides');
    const img = container.lastElementChild;
    if (!img || img.tagName !== 'IMG') return;

    const existing = container.querySelector('#modal-wrapper');
    if (existing) existing.remove();

    const wrapper = document.createElement('div');
    wrapper.id = 'modal-wrapper';
    wrapper.style.cssText = "position:absolute; top:0; left:0; width:100%; height:100%; pointer-events:none;";
    wrapper.innerHTML = `<svg id="modal-svg" style="width:100%; height:100%;"></svg><div id="modal-labels"></div>`;
    img.parentNode.appendChild(wrapper);

    const svg = wrapper.querySelector('#modal-svg');
    const labels = wrapper.querySelector('#modal-labels');

    ['a', 'b', 'c'].forEach(l => {
        const sx = card.dataset['sx' + l];
        const ex = card.dataset['ex' + l];

        if (!sx || sx === "0" || !ex || ex === "0") return;

        svg.insertAdjacentHTML('beforeend', `
            <line x1="${sx}%" y1="${card.dataset['sy' + l]}%" 
                  x2="${ex}%" y2="${card.dataset['ey' + l]}%" 
                  stroke="#DCAEAE" stroke-width="4" stroke-dasharray="6,6" />
        `);
        labels.insertAdjacentHTML('beforeend', `
            <div style="position:absolute; top:${card.dataset['my' + l]}%; left:${card.dataset['mx' + l]}%; 
            transform:translate(-50%,-50%); background:white; border:1px solid #000; padding:2px 5px; font-size:12px; font-weight:bold;">
            ${l.toUpperCase()}: ${card.dataset['val' + l] || ''} см</div>
        `);
    });
}
// 2. Сама форма отзыва (с ImgBB загрузкой)
window.openReviewModal = function (productId, productName) {
    Swal.fire({
        title: `ОТЗЫВ: ${productName.toUpperCase()}`,
        html: `
            <div class="review-form-container">
                <div class="star-rating-select" id="star-selector">
                    <i class="fa-solid fa-star selected" data-val="1"></i>
                    <i class="fa-solid fa-star selected" data-val="2"></i>
                    <i class="fa-solid fa-star selected" data-val="3"></i>
                    <i class="fa-solid fa-star selected" data-val="4"></i>
                    <i class="fa-solid fa-star selected" data-val="5"></i>
                </div>
                <textarea id="rev-comment" class="vesna-input-bold" placeholder="Как вам качество товара?" style="height: 120px;"></textarea>
                <label class="file-label">ПРИКРЕПИТЬ ФОТО</label>
                <input type="file" id="rev-image" class="vesna-input-bold" accept="image/*">
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'ОТПРАВИТЬ',
        customClass: { confirmButton: 'btn-black-caps', popup: 'vesna-popup' },
        didOpen: () => {
            const stars = document.querySelectorAll('#star-selector i');
            window.currentRating = 5;
            stars.forEach(s => s.onclick = () => {
                stars.forEach(i => i.classList.remove('selected'));
                let val = s.getAttribute('data-val');
                for (let i = 0; i < val; i++) stars[i].classList.add('selected');
                window.currentRating = val;
            });
        },
        preConfirm: () => {
            const formData = new FormData();
            formData.append('productId', productId);
            formData.append('rating', window.currentRating);
            formData.append('comment', document.getElementById('rev-comment').value);
            formData.append('image', document.getElementById('rev-image').files[0]);

            return fetch('/User/SubmitReview', { method: 'POST', body: formData })
                .then(res => res.json())
                .catch(err => Swal.showValidationMessage(`Ошибка: ${err}`));
        }
    }).then(result => {
        if (result.value?.success) {
            Swal.fire({ icon: 'success', title: 'ОПУБЛИКОВАНО', text: 'Спасибо за ваш отзыв!', showConfirmButton: false, timer: 2000 });
        }
    });
};