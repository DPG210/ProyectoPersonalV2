using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Models;

#region STORED PROCEDURE
/*
ALTER PROCEDURE [dbo].[SP_CREAR_USUARIO]
@nombre_usuario NVARCHAR(50), 
@email NVARCHAR(50), 
@password NVARCHAR(50), 
@password_hash NVARCHAR(100), 
@salt NVARCHAR(50),           
@token NVARCHAR(100),
@avatar NVARCHAR(50) 
AS
BEGIN
SET NOCOUNT ON;

IF EXISTS(SELECT 1 FROM USUARIOS WHERE nombre_usuario = @nombre_usuario)
BEGIN
    RAISERROR('El nombre de usuario ya existe', 16, 1);
    RETURN;
END

IF EXISTS(SELECT 1 FROM USUARIOS WHERE email = @email)
BEGIN
    RAISERROR('El email ya existe', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;
BEGIN TRY
    INSERT INTO USUARIOS(
        nombre_usuario, email, password, TokenMail, Activo, rol_id, fecha_registro,
        corazones_actuales, corazones_maximos, ultima_recarga, esta_banido, avatar
    )
    VALUES(
        @nombre_usuario, @email, @password, @token, 0, 1, GETUTCDATE(),
        5, 5, GETUTCDATE(), 0, @avatar
    );

    DECLARE @nuevo_id INT = SCOPE_IDENTITY();

    INSERT INTO user_security (id_usuario, password_hash, salt)
    VALUES (@nuevo_id, @password_hash, @salt);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH
END


ALTER PROCEDURE [dbo].[sp_CrearCuestionarioSimple]
@nombre_categoria NVARCHAR(50), 
@titulo NVARCHAR(150),
@descripcion NVARCHAR(MAX) = NULL,
@creador_id INT = NULL,
@es_publico BIT = 1 -- Nuevo parámetro: 1 para Público, 0 para Privado
AS
BEGIN
SET NOCOUNT ON;

DECLARE @cat_id INT;

-- 1. Buscamos el ID de la categoría por su nombre
SELECT @cat_id = categoria_id
FROM CATEGORIAS
WHERE nombre = @nombre_categoria;

-- 2. Si no existe la categoría, lanzamos un error claro
IF @cat_id IS NULL
BEGIN
    DECLARE @msg NVARCHAR(200) = 'Error: La categoría "' + @nombre_categoria + '" no existe en la base de datos.';
    RAISERROR(@msg, 16, 1);
    RETURN;
END

-- 3. Insertamos el cuestionario incluyendo la columna de visibilidad
INSERT INTO CUESTIONARIOS(categoria_id, titulo, descripcion, creador_id, es_publico, fecha_creacion)
VALUES(@cat_id, @titulo, @descripcion, @creador_id, @es_publico, GETUTCDATE());

-- 4. Devolvemos el ID del nuevo cuestionario
SELECT SCOPE_IDENTITY() AS NuevoCuestionarioID;
END
//
//   ALTER PROCEDURE [dbo].[sp_InsertarPreguntaQuiz]
//    @cuestionario_id INT, -- Ahora recibimos el ID directamente
//    @enunciado NVARCHAR(MAX),
//    @opcion_correcta NVARCHAR(MAX),
//    @opcion_incorrecta1 NVARCHAR(MAX),
//    @opcion_incorrecta2 NVARCHAR(MAX),
//    @opcion_incorrecta3 NVARCHAR(MAX),
//    @nivel INT,
//    @explicacion_didactica NVARCHAR(MAX) = NULL
//AS
//BEGIN
//    SET NOCOUNT ON;

//    -- Validamos que el cuestionario realmente existe
//    IF NOT EXISTS(SELECT 1 FROM CUESTIONARIOS WHERE cuestionario_id = @cuestionario_id)
//    BEGIN
//        RAISERROR('El ID de cuestionario proporcionado no existe.', 16, 1);
//    RETURN;
//    END

//    -- Insertamos en la tabla PREGUNTAS
//    INSERT INTO PREGUNTAS(
//        cuestionario_id,
//        enunciado,
//        respuesta_correcta,
//        opcion_incorrecta1,
//        opcion_incorrecta2,
//        opcion_incorrecta3,
//        NIVEL,
//        explicacion_didactica,
//        activa
//    )
//    VALUES(
//        @cuestionario_id,
//        @enunciado,
//        @opcion_correcta,
//        @opcion_incorrecta1,
//        @opcion_incorrecta2,
//        @opcion_incorrecta3,
//        @nivel,
//        @explicacion_didactica,
//        1
//    );
//    END
CREATE PROCEDURE sp_EnviarSolicitudAmistad
@id_emisor INT,
@id_receptor INT
AS
BEGIN
-- 1. Evitar enviarse a uno mismo
IF @id_emisor = @id_receptor
BEGIN
    RAISERROR('No puedes enviarte una invitación a ti mismo.', 16, 1);
    RETURN;
END

-- 2. Comprobar si ya existe una relación
IF EXISTS (SELECT 1 FROM AMISTADES 
           WHERE (usuario_id1 = @id_emisor AND usuario_id2 = @id_receptor) 
              OR (usuario_id1 = @id_receptor AND usuario_id2 = @id_emisor))
BEGIN
    -- Si existe y estaba RECHAZADA, la volvemos a poner en PENDIENTE
    UPDATE AMISTADES 
    SET estado = 'PENDIENTE', fecha_solicitud = GETDATE(), usuario_id1 = @id_emisor, usuario_id2 = @id_receptor
    WHERE (usuario_id1 = @id_emisor AND usuario_id2 = @id_receptor OR usuario_id1 = @id_receptor AND usuario_id2 = @id_emisor)
      AND estado = 'RECHAZADA';
END
ELSE
BEGIN
    -- 3. Si no existe nada, insertamos nuevo
    INSERT INTO AMISTADES (usuario_id1, usuario_id2, estado, fecha_solicitud)
    VALUES (@id_emisor, @id_receptor, 'PENDIENTE', GETDATE());
END
END
CREATE PROCEDURE sp_ResponderSolicitud
@id_emisor INT, -- El que la envió originalmente
@id_receptor INT, -- El que acepta (tú)
@nuevo_estado NVARCHAR(50) -- 'ACEPTADA' o 'RECHAZADA'
AS
BEGIN
UPDATE AMISTADES 
SET estado = @nuevo_estado 
WHERE usuario_id1 = @id_emisor AND usuario_id2 = @id_receptor;
END

ALTER PROCEDURE [dbo].[sp_UnirseAPartida]
@usuario_id INT,
@codigo_sala NVARCHAR(6)
AS
BEGIN
DECLARE @id_partida INT;

-- 1. Buscamos el ID de la partida que tenga ese código y esté en espera (LOBBY)
SELECT @id_partida = partida_id 
FROM partidas 
WHERE codigo_sala = @codigo_sala AND estado = 'esperando';

IF @id_partida IS NULL
BEGIN
    RAISERROR('La sala no existe o ya ha comenzado.', 16, 1);
    RETURN;
END

-- 2. Verificamos si el usuario ya está dentro (para evitar duplicados)
IF NOT EXISTS (SELECT 1 FROM PARTICIPANTES_PARTIDA WHERE partida_id = @id_partida AND usuario_id = @usuario_id)
BEGIN
    INSERT INTO PARTICIPANTES_PARTIDA (partida_id, usuario_id, puntuacion_actual, ha_terminado,indice_respondido)
    VALUES (@id_partida, @usuario_id, 0, 0,-1);
END

-- 3. Devolvemos el ID de la partida para que C# sepa a qué sala redirigir
SELECT @id_partida;
END
CREATE PROCEDURE [dbo].[sp_GuardarHistorialIndividual]
@usuario_id INT,
@nombre_cuestionario NVARCHAR(150),
@puntuacion INT,
@preguntas_correctas INT,
@preguntas_totales INT
AS
BEGIN
SET NOCOUNT ON;
DECLARE @c_id INT;

-- Buscamos el ID del cuestionario por su título
SELECT @c_id = cuestionario_id 
FROM CUESTIONARIOS 
WHERE titulo = @nombre_cuestionario;

-- Si lo encuentra, insertamos el historial
IF @c_id IS NOT NULL
BEGIN
    INSERT INTO HISTORIAL_INDIVIDUAL 
        (usuario_id, cuestionario_id, puntuacion, preguntas_correctas, preguntas_totales, fecha_finalizacion)
    VALUES 
        (@usuario_id, @c_id, @puntuacion, @preguntas_correctas, @preguntas_totales, GETUTCDATE());
END

CREATE PROCEDURE [dbo].[sp_FinalizarPartidaMulti]
@partida_id INT
AS
BEGIN
SET NOCOUNT ON;

-- Comprobamos que no esté ya finalizada para evitar recalcular
IF EXISTS (SELECT 1 FROM PARTIDAS WHERE partida_id = @partida_id AND estado != 'finalizada')
BEGIN
    -- A. Buscamos la puntuación máxima de esa partida concreta
    DECLARE @MaxPuntuacion INT;

    SELECT @MaxPuntuacion = MAX(puntuacion_actual)
    FROM PARTICIPANTES_PARTIDA
    WHERE partida_id = @partida_id;

    -- B. Marcamos como ganador a todo el que tenga esa puntuación máxima (soporta empates múltiples)
    UPDATE PARTICIPANTES_PARTIDA
    SET es_ganador = 1
    WHERE partida_id = @partida_id AND puntuacion_actual = @MaxPuntuacion;

    -- C. Cerramos la sala definitivamente
    UPDATE PARTIDAS 
    SET estado = 'finalizada' 
    WHERE partida_id = @partida_id;
END
END
ALTER PROCEDURE [dbo].[sp_CrearPartida]
@anfitrion_id INT,
@cuestionario_id INT,
@codigo_sala NVARCHAR(6),
@tipo_juego NVARCHAR(50),      
@cantidad_preguntas INT,       
@tiempo_pregunta INT,          
@es_publica BIT,
@capacidad_maxima INT          -- ¡NUEVO PARÁMETRO!
AS
BEGIN
SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @id_partida INT;

    -- Insertamos la partida con la nueva columna [capacidad_maxima]
    INSERT INTO partidas 
        (anfitrion_id, cuestionario_id, codigo_sala, estado, fecha_creacion, tipo_juego, cantidad_preguntas, tiempo_pregunta, es_publica, capacidad_maxima)
    VALUES 
        (@anfitrion_id, @cuestionario_id, @codigo_sala, 'esperando', GETDATE(), @tipo_juego, @cantidad_preguntas, @tiempo_pregunta, @es_publica, @capacidad_maxima);

    SET @id_partida = SCOPE_IDENTITY();

    -- Insertamos al anfitrión
    INSERT INTO PARTICIPANTES_PARTIDA (partida_id, usuario_id, puntuacion_actual, ha_terminado)
    VALUES (@id_partida, @anfitrion_id, 0, 0);

    COMMIT TRANSACTION;
    SELECT @id_partida;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH
END
CREATE PROCEDURE [dbo].[sp_GuardarRankingModo]
@usuario_id INT,
@modo_juego NVARCHAR(50),
@puntos INT
AS
BEGIN
SET NOCOUNT ON;

INSERT INTO RANKING_MODOS (usuario_id, modo_juego, puntos, fecha_partida)
VALUES (@usuario_id, @modo_juego, @puntos, GETUTCDATE());
END
GO
CREATE PROCEDURE [dbo].[sp_EliminarUsuario]
@usuario_id INT
AS
BEGIN
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. PRESERVAR EL CONTENIDO: Reasignar los cuestionarios al "creador 0" (Sistema)
    UPDATE CUESTIONARIOS
    SET creador_id = 0
    WHERE creador_id = @usuario_id;

    -- 2. PRESERVAR PARTIDAS (Opcional): Si era anfitrión de partidas que no han terminado, 
    -- puedes borrarlas o reasignarlas. Aquí las borramos por seguridad.
    DELETE FROM PARTIDAS WHERE anfitrion_id = @usuario_id;

    -- 3. LIMPIEZA DE DEPENDENCIAS: Borrar todo rastro del usuario en otras tablas
    -- (Si ya tienes configurado 'ON DELETE CASCADE' en tu BD, podrías omitir este paso, 
    -- pero ponerlo explícitamente evita errores de integridad referencial).
    DELETE FROM USER_SECURITY WHERE id_usuario = @usuario_id;
    DELETE FROM SESIONES WHERE usuario_id = @usuario_id;
    DELETE FROM AMISTADES WHERE usuario_id1 = @usuario_id OR usuario_id2 = @usuario_id;
    DELETE FROM INVITACIONES WHERE usuario_emisor_id = @usuario_id OR usuario_receptor_id = @usuario_id;
    DELETE FROM USUARIOS_LOGROS WHERE usuario_id = @usuario_id;
    DELETE FROM HISTORIAL_INDIVIDUAL WHERE usuario_id = @usuario_id;
    DELETE FROM HISTORIAL_SANCIONES WHERE usuario_id = @usuario_id;
    DELETE FROM RANKING_MODOS WHERE usuario_id = @usuario_id;
    DELETE FROM REPORTES_PREGUNTAS WHERE usuario_id = @usuario_id;
    DELETE FROM PARTICIPANTES_PARTIDA WHERE usuario_id = @usuario_id;
    DELETE FROM USUARIOS_RESPUESTAS WHERE usuario_id = @usuario_id;

    -- 4. ELIMINACIÓN FINAL: Borrar al usuario de la tabla principal
    DELETE FROM USUARIOS
    WHERE usuario_id = @usuario_id;

    COMMIT TRANSACTION;

    -- Mensaje de retorno para confirmar en C#
    SELECT 'EXITO' AS Resultado, 'Usuario eliminado y cuestionarios reasignados al sistema.' AS Mensaje;
END TRY
BEGIN CATCH
    -- Si falla CUALQUIER paso anterior, se deshace todo para no dejar la BD a medias
    IF @@TRANCOUNT > 0 
        ROLLBACK TRANSACTION;

    -- Lanzar el error para que tu backend pueda capturarlo
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrorMessage, 16, 1);
END CATCH
END;
GO
CREATE PROCEDURE [dbo].[sp_CambiarPassword]
@p_usuario_id INT,
@p_nuevo_password NVARCHAR(255),
@p_nuevo_hash NVARCHAR(100),
@p_nuevo_salt NVARCHAR(100)
AS
BEGIN
BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Actualizamos la tabla principal (Password y vaciamos el TokenMail)
    UPDATE USUARIOS
    SET 
        password = @p_nuevo_password,
        TokenMail = NULL
    WHERE usuario_id = @p_usuario_id;

    -- 2. Actualizamos la tabla de seguridad (Hash y Salt)
    UPDATE USER_SECURITY
    SET 
        password_hash = @p_nuevo_hash,
        salt = @p_nuevo_salt
    WHERE id_usuario = @p_usuario_id;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    -- Si algo falla, deshacemos los cambios para no romper la cuenta
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
END;
GO
*/
#endregion


namespace ProyectoPersonal.Repositories
{
    public class RepositoryJuego : IRepositoryJuego
    {
        private TrivialContext context;
        public RepositoryJuego(TrivialContext context)
        {
            this.context = context;
        }
        public async Task RegistrarRespuestaAsync(int partidaId, int usuarioId, int indicePregunta, bool esCorrecta, int puntos)
        {
            string sql = @"UPDATE PARTICIPANTES_PARTIDA 
                   SET puntuacion_actual = puntuacion_actual + @puntos,
                       indice_respondido = @indice
                   WHERE partida_id = @pid AND usuario_id = @uid";


            SqlParameter pamPun = new SqlParameter("@puntos", puntos);
            SqlParameter pamInd = new SqlParameter("@indice", indicePregunta);
            SqlParameter pamPartId = new SqlParameter("@pid", partidaId);
            SqlParameter pamIdU = new SqlParameter("@uid", usuarioId);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamPun, pamInd, pamPartId, pamIdU);
        }
        public async Task<bool> TodosHanRespondidoAsync(int partidaId, int indicePregunta)
        {
            var participantes = from datos in this.context.ParticipantesPartida
                                where datos.IdPartida == partidaId
                                select datos;

            bool existenParticipantes = await participantes.AnyAsync();
            bool todosListos = await participantes.AllAsync(p => p.IndiceRespondido >= indicePregunta);

            return existenParticipantes && todosListos;
        }

        public async Task<List<RankingJugador>> GetRankingAsync(int partidaId)
        {
            var consulta = from p in this.context.ParticipantesPartida
                           join u in this.context.Usuarios on p.IdUsuario equals u.IdUsuario
                           where p.IdPartida == partidaId
                           orderby p.PuntuacionActual descending 
                           select new RankingJugador
                           {
                               Nombre = u.Nombre,
                               Puntos = p.PuntuacionActual
                           };

            return await consulta.ToListAsync();
        }
        public async Task AvanzarJugadorAsync(int partidaId, int usuarioId, int nuevoIndice)
        {
            string sql = @"UPDATE PARTICIPANTES_PARTIDA 
                   SET indice_respondido = @indice
                   WHERE partida_id = @pid AND usuario_id = @uid";
            SqlParameter pamInd = new SqlParameter("@indice", nuevoIndice);
            SqlParameter pamPartId = new SqlParameter("@pid", partidaId);
            SqlParameter pamIdUsu = new SqlParameter("@uid", usuarioId);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamInd, pamPartId, pamIdUsu);
        }
        public async Task GuardarHistorialIndividualAsync(int idUsuario, string nombreCuestionario, int puntuacion, int correctas, int totales)
        {
            string sql = "sp_GuardarHistorialIndividual @usuario_id,@nombre_cuestionario,@puntuacion,@preguntas_correctas,@preguntas_totales";
            SqlParameter pamIdUsu = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamCue = new SqlParameter("@nombre_cuestionario", nombreCuestionario);
            SqlParameter pamPun = new SqlParameter("@puntuacion", puntuacion);
            SqlParameter pamPreC = new SqlParameter("@preguntas_correctas", correctas);
            SqlParameter pamPreT = new SqlParameter("@preguntas_totales", totales);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamIdUsu, pamCue, pamPun, pamPreC, pamPreT);
        }


        public async Task<List<HistorialIndividualPartidas>> GetHistorialIndividualAsync(int idUsuario)
        {
            var consulta = from h in this.context.HistorialIndividual
                           join c in this.context.Cuestionarios on h.IdCuestionario equals c.IdCuestionario
                           where h.IdUsuario == idUsuario
                           orderby h.FechaFinalizacion descending
                           select new HistorialIndividualPartidas
                           {
                               IdUsuario = h.IdUsuario,
                               IdCuestionario = h.IdCuestionario,
                               NombreCuestionario = c.Titulo,
                               Puntuacion = h.Puntuacion,
                               PreguntasCorrectas = h.PreguntasCorrectas,
                               PreguntasTotales = h.PreguntasTotales,
                               FechaFinalizacion = h.FechaFinalizacion
                           };

            return await consulta.ToListAsync();
        }
        public async Task<List<HistorialMultiPartida>> GetHistorialMultijugadorAsync(int idUsuario)
        {
            var consultaPartidas = from p in this.context.Partidas
                                   join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                                   join pp in this.context.ParticipantesPartida on p.IdPartida equals pp.IdPartida
                                   where pp.IdUsuario == idUsuario && p.Estado == "finalizada"
                                   orderby p.IdPartida descending
                                   select new HistorialMultiPartida
                                   {
                                       IdPartida = p.IdPartida,
                                       NombreCuestionario = c.Titulo
                                   };

            List<HistorialMultiPartida> historial = await consultaPartidas.ToListAsync();

            foreach (var partida in historial)
            {
                partida.Participantes = await (from pp in this.context.ParticipantesPartida
                                               join u in this.context.Usuarios on pp.IdUsuario equals u.IdUsuario
                                               where pp.IdPartida == partida.IdPartida
                                               orderby pp.PuntuacionActual descending
                                               select new ParticipantePartida
                                               {
                                                   IdUsuario = u.IdUsuario,
                                                   Nombre = u.Nombre,
                                                   Puntuacion = pp.PuntuacionActual,
                                                   HaRespondido = true
                                               }).ToListAsync();

                if (partida.Participantes.Any())
                {
                    partida.Ganador = partida.Participantes.First().Nombre;
                }
            }
            return historial;
        }

        public async Task GuardarRankingModoAsync(int idUsuario, string modoJuego, int puntos, string nombreCuestionario)
        {
            string sql = "sp_GuardarRankingModo @usuario_id, @modo_juego, @puntos,@nombre_cuestionario";

            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamModo = new SqlParameter("@modo_juego", modoJuego);
            SqlParameter pamPuntos = new SqlParameter("@puntos", puntos);
            SqlParameter pamCuestionario = new SqlParameter("@nombre_cuestionario", nombreCuestionario);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamModo, pamPuntos, pamCuestionario);
        }


        public async Task<List<RankingModo>> GetRankingsPorUsuarioAsync(int idUsuario)
        {
            var consulta = from r in this.context.RankingModos
                           join c in this.context.Cuestionarios
                           on r.IdCuestionario equals c.IdCuestionario 
                           where r.IdUsuario == idUsuario
                           orderby r.Puntos descending
                           select new RankingModo
                           {
                               IdUsuario = r.IdUsuario,
                               ModoJuego = r.ModoJuego,
                               IdCuestionario = r.IdCuestionario,
                               Puntos = r.Puntos,
                               FechaPartida = r.FechaPartida,
                               NombreCuestionario = c.Titulo 
                           };

            return await consulta.ToListAsync();
        }




        public async Task<List<dynamic>> GetTopRankingGlobalAsync(string modo, string filtro, string orden)
        {

            var consulta = from rank in this.context.RankingModos
                           join user in this.context.Usuarios on rank.IdUsuario equals user.IdUsuario
                           join quest in this.context.Cuestionarios on rank.IdCuestionario equals quest.IdCuestionario
                           join cat in this.context.Categorias on quest.IdCategoria equals cat.IdCategoria
                           where rank.ModoJuego.ToLower() == modo.ToLower()
                           select new
                           {
                               nombre = user.Nombre,
                               avatar = user.Avatar,
                               puntos = rank.Puntos,
                               cuestionario = quest.Titulo,
                               fecha = rank.FechaPartida,
                               nombreCategoria = cat.Nombre 
                           };

            if (!string.IsNullOrEmpty(filtro))
            {
                consulta = consulta.Where(r => r.nombreCategoria == filtro);
            }

            if (orden == "asc")
            {
                consulta = consulta.OrderBy(r => r.puntos);
            }
            else
            {
                consulta = consulta.OrderByDescending(r => r.puntos);
            }

            var resultados = await consulta.Take(10).ToListAsync();
            return resultados.Cast<dynamic>().ToList();
        }

        
    }
}
