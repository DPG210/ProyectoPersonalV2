// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let adTimerInterval;

function abrirAnuncio() {
    const modal = document.getElementById('modalAnuncio');
    const video = document.getElementById('videoAnuncio');
    const source = document.getElementById('videoSource');
    const btnCerrar = document.getElementById('btnCerrarAnuncio');
    const txtContador = document.getElementById('txtContadorAnuncio');

    // 1. Selección aleatoria del vídeo
    const numeroAleatorio = Math.floor(Math.random() * 2) + 1;
    const rutaVideo = `/videos/anuncio${numeroAleatorio}.mp4`;

    source.src = rutaVideo;
    video.load();

    // 2. Reseteo visual
    let tiempoRestante = 15;
    modal.classList.remove('hidden');
    btnCerrar.classList.add('hidden');
    txtContador.classList.remove('text-emerald-400');
    txtContador.classList.add('text-white');
    txtContador.innerText = `El anuncio se puede saltar en ${tiempoRestante}s`;

    video.currentTime = 0;
    video.play().catch(e => console.log("Autoplay bloqueado"));

    if (adTimerInterval) clearInterval(adTimerInterval);

    // 3. Cuenta atrás independiente del vídeo
    adTimerInterval = setInterval(() => {
        tiempoRestante--;
        if (tiempoRestante > 0) {
            txtContador.innerText = `El anuncio se puede saltar en ${tiempoRestante}s`;
        } else {
            // Pasaron los 15s. Mostramos la X, PERO NO pausamos el vídeo.
            clearInterval(adTimerInterval);
            txtContador.innerText = "¡Recompensa lista!";
            txtContador.classList.replace('text-white', 'text-emerald-400');
            btnCerrar.classList.remove('hidden');
            btnCerrar.classList.add('animate__animated', 'animate__bounceIn');
        }
    }, 1000);
}

async function cerrarYReclamarAnuncio() {
    const modal = document.getElementById('modalAnuncio');
    const video = document.getElementById('videoAnuncio');

    modal.classList.add('hidden');
    video.pause();

    try {
        const response = await fetch('/Datos/ReclamarCorazonAnuncio', {
            method: 'POST'
        });
        const data = await response.json();

        if (data.success) {
            Swal.fire({
                title: '¡Recompensa Obtenida!',
                text: data.mensaje,
                icon: 'success',
                confirmButtonColor: '#10b981',
                background: document.documentElement.classList.contains('dark') ? '#0f172a' : '#ffffff',
                color: document.documentElement.classList.contains('dark') ? '#f1f5f9' : '#0f172a'
            }).then(() => {

                // AQUÍ ES DONDE VA LA MAGIA (dentro del .then de Swal)
                const contenedorCorazones = document.getElementById('contador-corazones');

                if (contenedorCorazones) {
                    let corazonesSpans = '';

                    for (let i = 0; i < 5; i++) {
                        if (i < data.corazones) {
                            corazonesSpans += `<span class="text-2xl drop-shadow-md transform hover:scale-125 transition-transform cursor-pointer animate__animated animate__heartBeat">❤️</span>`;
                        } else {
                            corazonesSpans += `<span class="text-2xl grayscale opacity-20 dark:opacity-30">🤍</span>`;
                        }
                    }

                    contenedorCorazones.innerHTML = `
                        <div class="flex items-center gap-4 bg-rose-50 dark:bg-rose-900/20 px-6 py-2.5 rounded-[2rem] border border-rose-100 dark:border-rose-800/50 shadow-inner transition-colors duration-300">
                            <span class="text-[10px] font-black text-rose-500 dark:text-rose-400 uppercase tracking-widest hidden sm:block">Suministro Vital</span>
                            <div class="flex gap-1.5">
                                ${corazonesSpans}
                            </div>
                        </div>
                    `;
                }

            });

        } else {
            Swal.fire({
                title: 'No se pudo reclamar',
                text: data.mensaje,
                icon: 'info',
                confirmButtonColor: '#6366f1'
            });
        }
    } catch (error) {
        console.error("Error al reclamar el anuncio:", error);
    }
}
