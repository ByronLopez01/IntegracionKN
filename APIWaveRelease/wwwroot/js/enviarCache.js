// Se envuelve todo en un evento DOMContentLoaded para asegurar que el HTML esté cargado.
document.addEventListener('DOMContentLoaded', () => {

    // Obtenemos la contraseña del admin desde un atributo de datos y la parseamos como JSON.
    const adminPasswordJSON = document.querySelector('#mainContent').dataset.adminPassword;
    const adminPassword = JSON.parse(adminPasswordJSON);
    let isAdminAuthenticated = false;

    const icons = {
        locked: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-lock-fill" viewBox="0 0 16 16"><path d="M8 1a2 2 0 0 1 2 2v4H6V3a2 2 0 0 1 2-2m3 6V3a3 3 0 0 0-6 0v4a2 2 0 0 0-2 2v5a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2"/></svg>`,
        unlocked: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-unlock-fill" viewBox="0 0 16 16"><path d="M11 1a2 2 0 0 0-2 2v4a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h5V3a3 3 0 0 1 6 0v4a.5.5 0 0 1-1 0V3a2 2 0 0 0-2-2"/></svg>`
    };

    const globalLoader = document.getElementById('globalLoaderOverlay');

    // --- Asignación de eventos y funciones ---
    // Se asignan aquí para asegurar que los elementos existan.

    document.getElementById('username').addEventListener('keydown', handleLoginKeydown);
    document.getElementById('password').addEventListener('keydown', handleLoginKeydown);
    document.querySelector('#loginModal button').onclick = validarCredenciales;

    document.getElementById('myForm').addEventListener('submit', handleEnviarCacheSubmit);

    document.getElementById('eliminarCacheBtn').onclick = eliminarCache;
    document.getElementById('cerrarWaveBtn').onclick = cerrarWave;

    document.getElementById('tabEnviar').onclick = () => showTab('enviar');
    document.getElementById('tabExtra').onclick = showAdminTab;

    document.getElementById('adminLockIcon').innerHTML = icons.locked;


    function handleLoginKeydown(event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            validarCredenciales();
        }
    }

    async function validarCredenciales() {
        document.querySelectorAll('.validation-message').forEach(e => e.textContent = '');
        document.getElementById('loginMessage').textContent = '';

        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value.trim();

        if (!username || !password) {
            if (!username) document.getElementById('userError').textContent = 'Usuario es requerido';
            if (!password) document.getElementById('passError').textContent = 'Contraseña es requerida';
            return;
        }

        try {
            const response = await fetch('/api/WaveRelease/ValidarUsuario', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Usuario: username, Contrasena: password })
            });

            if (response.ok) {
                document.getElementById('loginModal').style.display = 'none';
                document.getElementById('mainContent').style.display = 'flex';
                showTab('enviar');
            } else {
                document.getElementById('loginMessage').textContent = "Credenciales incorrectas";
            }
        } catch (error) {
            document.getElementById('loginMessage').textContent = "Error de conexión";
        }
    }

    async function handleEnviarCacheSubmit(e) {
        e.preventDefault();

        const btn = document.getElementById('submitBtn');
        const messageContainer = document.getElementById('messageContainer');
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        btn.disabled = true;
        globalLoader.style.display = 'flex';
        messageContainer.innerHTML = '';

        try {
            const response = await fetch('/api/WaveRelease/EnviarCache', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'Authorization': 'Basic ' + btoa('senad:S3nad'),
                    'Content-Type': 'application/json'
                }
            });

            const texto = await response.text();
            const data = {
                mensaje: `${response.status} - ${texto}`,
                esError: !response.ok
            };

            const alertType = data.esError ? "alert-danger" : "alert-success";
            messageContainer.innerHTML = `<div class="alert ${alertType}">${data.mensaje}</div>`;

        } catch (error) {
            messageContainer.innerHTML = `<div class="alert alert-danger">Error de conexión al servidor: ${error.message}</div>`;
        } finally {
            btn.disabled = false;
            globalLoader.style.display = 'none';
            await actualizarWaveStatus();
            await actualizarWaveReleaseStatus();
        }
    }

    async function eliminarCache() {
        const messageContainer = document.getElementById('messageContainerExtra');
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const btn = document.getElementById('eliminarCacheBtn');

        let cacheInfo;
        try {
            const response = await fetch('/api/WaveRelease/ObtenerNombreWaveCache');
            if (!response.ok) throw new Error('No se pudo verificar el estado de la caché.');
            cacheInfo = await response.json();
        } catch (error) {
            messageContainer.innerHTML = `<div class="alert alert-danger">Error al verificar la caché: ${error.message}</div>`;
            return;
        }

        if (!cacheInfo.existe) {
            showCustomAlert("Información", "La WaveCache ya está limpia. No hay datos para eliminar.");
            return;
        }

        const confirmacion = await showCustomConfirm("Confirmar Eliminación", `¿Estás seguro de que deseas eliminar todos los datos de la wave (${cacheInfo.nombre})? Esta acción no se puede deshacer.`);
        if (!confirmacion) return;

        btn.disabled = true;
        globalLoader.style.display = 'flex';
        messageContainer.innerHTML = '';

        try {
            const response = await fetch('/api/WaveRelease/EliminarCache', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'Authorization': 'Basic ' + btoa('senad:S3nad'),
                    'Content-Type': 'application/json'
                }
            });

            const texto = await response.text();
            const data = { mensaje: `${response.status} - ${texto}`, esError: !response.ok };
            const alertType = data.esError ? "alert-danger" : "alert-success";
            messageContainer.innerHTML = `<div class="alert ${alertType}">${data.mensaje}</div>`;
        } catch (error) {
            messageContainer.innerHTML = `<div class="alert alert-danger">Error de conexión al servidor: ${error.message}</div>`;
        } finally {
            btn.disabled = false;
            globalLoader.style.display = 'none';
            await actualizarWaveStatus();
            await actualizarWaveReleaseStatus();
        }
    }

    async function cerrarWave() {
        const confirmacion = await showCustomConfirm("Confirmar Cierre", "¿Estás seguro de que deseas cerrar la WaveRelease activa? Esto afectará a todas las órdenes en proceso.");
        if (!confirmacion) return;

        const messageContainer = document.getElementById('messageContainerExtra');
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const btn = document.getElementById('cerrarWaveBtn');

        btn.disabled = true;
        globalLoader.style.display = 'flex';
        messageContainer.innerHTML = '';

        try {
            const response = await fetch('/api/WaveRelease/CerrarWave', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'Authorization': 'Basic ' + btoa('senad:S3nad'),
                    'Content-Type': 'application/json'
                }
            });
            const texto = await response.text();
            const data = { mensaje: `${response.status} - ${texto}`, esError: !response.ok };
            const alertType = data.esError ? "alert-danger" : "alert-success";
            messageContainer.innerHTML = `<div class="alert ${alertType}">${data.mensaje}</div>`;
        } catch (error) {
            messageContainer.innerHTML = `<div class="alert alert-danger">Error de conexión al servidor: ${error.message}</div>`;
        } finally {
            btn.disabled = false;
            globalLoader.style.display = 'none';
            await actualizarWaveStatus();
            await actualizarWaveReleaseStatus();
        }
    }

    const modalOverlay = document.getElementById('customModalOverlay');
    const modalTitle = document.getElementById('customModalTitle');
    const modalMessage = document.getElementById('customModalMessage');
    const modalActions = document.getElementById('customModalActions');

    function showCustomAlert(title, message) {
        modalTitle.textContent = title;
        modalMessage.textContent = message;
        modalActions.innerHTML = '<button class="custom-button custom-button-filled">Aceptar</button>';
        modalOverlay.style.display = 'flex';
        modalActions.querySelector('button').onclick = () => {
            modalOverlay.style.display = 'none';
        };
    }

    function showCustomConfirm(title, message) {
        return new Promise(resolve => {
            modalTitle.textContent = title;
            modalMessage.textContent = message;
            modalActions.innerHTML = `
                <button id="confirmBtn" class="custom-button custom-button-filled">Confirmar</button>
                <button id="cancelBtn" class="custom-button custom-button-filled custom-button-danger">Cancelar</button>
            `;
            modalOverlay.style.display = 'flex';
            document.getElementById('confirmBtn').onclick = () => {
                modalOverlay.style.display = 'none';
                resolve(true);
            };
            document.getElementById('cancelBtn').onclick = () => {
                modalOverlay.style.display = 'none';
                resolve(false);
            };
        });
    }

    function showAdminPasswordPrompt() {
        return new Promise(resolve => {
            modalTitle.textContent = 'Acceso Restringido';
            modalMessage.innerHTML = `
                <p>Por favor, ingrese la contraseña para acceder a las opciones de administrador.</p>
                <div class="form-group">
                    <input class="input-modal" type="password" id="adminPassword" placeholder="Contraseña" required>
                    <div class="validation-message" id="adminPassError"></div>
                </div>
            `;
            modalActions.innerHTML = `
                <button id="adminConfirmBtn" class="custom-button custom-button-filled">Ingresar</button>
                <button id="adminCancelBtn" class="custom-button custom-button-filled custom-button-danger">Cancelar</button>
            `;

            const modalContent = document.getElementById('customModalContent');
            modalContent.style.maxWidth = '300px';
            modalOverlay.style.display = 'flex';

            const adminPasswordField = document.getElementById('adminPassword');
            const adminPassError = document.getElementById('adminPassError');

            const handleConfirm = () => {
                if (adminPasswordField.value === adminPassword) { // Usar la variable JS
                    modalOverlay.style.display = 'none';
                    resolve(true);
                } else {
                    adminPassError.textContent = 'Contraseña incorrecta.';
                }
            };

            document.getElementById('adminConfirmBtn').onclick = handleConfirm;
            adminPasswordField.addEventListener('keydown', (event) => {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    handleConfirm();
                }
            });
            document.getElementById('adminCancelBtn').onclick = () => {
                modalOverlay.style.display = 'none';
                resolve(false);
            };
        });
    }

    function showTab(tabName) {
        document.querySelectorAll('.tab-content').forEach(content => content.style.display = 'none');
        document.querySelectorAll('.tab').forEach(tab => tab.classList.remove('active'));

        if (tabName === 'enviar') {
            document.getElementById('tabContentEnviar').style.display = 'flex';
            document.getElementById('tabEnviar').classList.add('active');
        } else if (tabName === 'extra' && isAdminAuthenticated) {
            document.getElementById('tabContentExtra').style.display = 'flex';
            document.getElementById('tabExtra').classList.add('active');
        }
    }

    async function showAdminTab() {
        if (isAdminAuthenticated) {
            showTab('extra');
            return;
        }
        const isAuthenticated = await showAdminPasswordPrompt();
        if (isAuthenticated) {
            isAdminAuthenticated = true;
            document.getElementById('adminLockIcon').innerHTML = icons.unlocked;
            showTab('extra');
        }
    }

    async function actualizarWaveStatus() {
        try {
            const response = await fetch('/api/WaveRelease/WaveStatus');
            if (response.ok) {
                document.getElementById('waveStatusContainer').innerHTML = await response.text();
            } else {
                console.error('Error al actualizar el estado de la wave.');
            }
        } catch (error) {
            console.error('Error de conexión al actualizar el estado de la wave:', error);
        }
    }

    async function actualizarWaveReleaseStatus() {
        try {
            const response = await fetch('/api/WaveRelease/WaveReleaseStatus');
            if (response.ok) {
                document.getElementById('waveReleaseStatusContainer').innerHTML = await response.text();
            } else {
                console.error('Error al actualizar el estado de WaveRelease.');
            }
        } catch (error) {
            console.error('Error de conexión al actualizar el estado de WaveRelease:', error);
        }
    }
});