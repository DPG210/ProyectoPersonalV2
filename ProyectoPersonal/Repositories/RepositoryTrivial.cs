using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProyectoPersonal.Data;
using ProyectoPersonal.Helpers;
using ProyectoPersonal.Models;
using System;
using System.Data;
using System.Data.Common;
using static Azure.Core.HttpHeader;
using static ProyectoPersonal.Models.HistorialMultiPartida;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProyectoPersonal.Repositories
    
{
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


    public class RepositoryTrivial
    {
        private TrivialContext context;
        
        public RepositoryTrivial(TrivialContext context)
        {
            this.context = context;
        }

        public async Task<List<string>> GetCategoriasAsync()
        {
            var consulta = (from datos in this.context.Categorias
                            select datos.Nombre).Distinct();

            return await consulta.ToListAsync();

        }
        public async Task<int> GetIdCuestionarioByNombreAsync(string nombre)
        {
            var consulta = from datos in this.context.Cuestionarios
                            where datos.Titulo == nombre
                            select datos.IdCuestionario;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetCuestionariosAsync(string nombreCategoria, int idUsuario)
        {
            var consultaCategoria = from datos in this.context.Categorias
                                    where datos.Nombre == nombreCategoria
                                    select datos.IdCategoria;
            int idCategoria = await consultaCategoria.FirstOrDefaultAsync();
            

            var consultaTitulo = from datos in this.context.Cuestionarios
                                 where datos.IdCategoria == idCategoria &&
                                (datos.EsPublico == true ||
                                 datos.CreadorId == idUsuario)
                                 select datos.Titulo;
            return await consultaTitulo.ToListAsync();
        }
        public async Task<string> FindCuestionarioAsync(int idCuestionario)
        {
            var consulta= from datos in this.context.Cuestionarios
                          where datos.IdCuestionario == idCuestionario
                          select datos.Titulo;
            
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetAllNombresCuestionariosPublicosAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Cuestionarios
                           where datos.EsPublico == true
                           || datos.CreadorId== idUsuario
                           select datos.Titulo;

            return await consulta.ToListAsync();
        }
        public async Task<List<Cuestionario>> GetCuestionariosUsuarioAsync(int idUsuario)
        {
            return await this.context.Cuestionarios
                .Where(z => z.CreadorId == idUsuario)
                .ToListAsync();
        }
        public async Task<List<Cuestionario>> GetCuestionarioCompletoAsync(string nombreCategoria, int idUsuario)
        {
            var consultaCategoria = from datos in this.context.Categorias
                                    where datos.Nombre == nombreCategoria
                                    select datos.IdCategoria;
            
            int idCategoria = await consultaCategoria.FirstOrDefaultAsync();
            var consultaCuestionario = from datos in this.context.Cuestionarios
                           where datos.IdCategoria == idCategoria &&
                          (datos.EsPublico == true ||
                           datos.CreadorId == idUsuario)
                           select datos;
            
            return await consultaCuestionario.ToListAsync();
        }


        public async Task CreateUsuario(string nombre, string email, string password, string token,string salt, string pass_hash, string avatar)
        {
            string sql = "SP_CREAR_USUARIO @nombre_usuario,@email,@password,@password_hash,@salt,@token,@avatar";
            SqlParameter pamNom= new SqlParameter ("@nombre_usuario", nombre);
            SqlParameter pamEm = new SqlParameter("@email", email);
            SqlParameter pamPass = new SqlParameter("@password", password);
            SqlParameter pamTok = new SqlParameter("@token", token);
            SqlParameter pamSalt = new SqlParameter("@salt", salt);
            SqlParameter pamPassH = new SqlParameter("@password_hash", pass_hash);
            SqlParameter pamAvatar = new SqlParameter("@avatar", avatar);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamNom, pamEm, pamPass, pamPassH, pamSalt, pamTok, pamAvatar);
        }
        public async Task<List<Usuario>> GetUsuariosASync()
        {
            var consulta = from datos in this.context.Usuarios
                           select datos;
            return await consulta.ToListAsync();
        }
        public async Task<Usuario> LoginUsuarioAsync(string username, string password)
        {
            var consulta= from datos in this.context.Usuarios
                           where datos.Email == username || datos.Nombre == username
                          select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();
            
            if (usuario == null) return null;

            var consultaSeguridad = from datos in this.context.Logins
                            where datos.IdUsuario == usuario.IdUsuario
                            select datos;
            Login seguridad = await consultaSeguridad.FirstOrDefaultAsync();

            if (seguridad == null) return null;
            
            string passwordConSalt = password + seguridad.Salt;
            string hashCalculado = HelperCryptography.EncriptarTextoBasico(passwordConSalt);
            
            if (hashCalculado == seguridad.PasswordHash)
            {
                return usuario; 
            }

            return null;
        }
        public async Task<bool> ActivarCuentaAsync(string token)
        {
            // 1. Buscamos al usuario que tenga exactamente ese token
            var usuario = await this.context.Usuarios
                                    .FirstOrDefaultAsync(u => u.TokenMail == token);

            // 2. Si lo encontramos, le cambiamos los datos
            if (usuario != null)
            {
                usuario.Activo = true; // ¡Cuenta activada!
                usuario.TokenMail = null; // Borramos el token por seguridad para que no se re-use

                await this.context.SaveChangesAsync(); // Guardamos los cambios en SQL
                return true;
            }

            // Si no lo encuentra (token inventado o ya usado)
            return false;
        }
        public async Task CreateCuestionarioAsync(string categoria, string titulo, string descripcion, int idUsuario, bool esPublico)
        {
            string sql = "sp_CrearCuestionarioSimple @nombre_categoria,@titulo,@descripcion,@creador_id,@es_publico";
            SqlParameter pamCat= new SqlParameter("@nombre_categoria", categoria);
            SqlParameter pamTit = new SqlParameter("@titulo", titulo);
            SqlParameter pamDes = new SqlParameter("@descripcion", descripcion);
            SqlParameter pamCre = new SqlParameter("@creador_id", idUsuario);
            SqlParameter pamPub = new SqlParameter("@es_publico", esPublico);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamCat, pamTit, pamDes, pamCre, pamPub);
        }

        public async Task InsertPreguntasAsync
            (int idCuestionario, string enunciado, string opc_correct, string opc_incorrect_1, string opc_incorrect_2, string opc_incorrect_3, int nivel, string? explicacion)
        {

            string sql = "sp_InsertarPreguntaQuiz @cuestionario_id,@enunciado,@opcion_correcta," +
                "@opcion_incorrecta1,@opcion_incorrecta2,@opcion_incorrecta3,@nivel,@explicacion_didactica";
            SqlParameter pamCue = new SqlParameter("@cuestionario_id", idCuestionario);
            SqlParameter pamEnu = new SqlParameter("@enunciado", enunciado);
            SqlParameter pamOpc= new SqlParameter("@opcion_correcta", opc_correct);
            SqlParameter pamOpi1= new SqlParameter("@opcion_incorrecta1", opc_incorrect_1);
            SqlParameter pamOpi2 = new SqlParameter("@opcion_incorrecta2", opc_incorrect_2);
            SqlParameter pamOpi3 = new SqlParameter("@opcion_incorrecta3", opc_incorrect_3);
            SqlParameter pamNiv= new SqlParameter("@nivel", nivel);
            SqlParameter pamExp= new SqlParameter("@explicacion_didactica", explicacion);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamCue, pamEnu,pamOpc,pamOpi1,pamOpi2,pamOpi3,pamNiv,pamExp);
        }
        public async Task CambiarAvatarAsync(int idUsuario, string nuevoAvatar)
        {
            string sql = "UPDATE USUARIOS SET avatar = @avatar WHERE usuario_id = @id";
            SqlParameter pamAv = new SqlParameter("@avatar", nuevoAvatar);
            SqlParameter pamId = new SqlParameter("@id", idUsuario);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamAv, pamId);
        }
        public async Task EnviarSolicitudAsync(int idEmisor, int idReceptor)
        {
            string sql = "sp_EnviarSolicitudAmistad @id_emisor,@id_receptor";
            SqlParameter pamEmi = new SqlParameter("@id_emisor", idEmisor);
            SqlParameter pamRec = new SqlParameter("@id_receptor", idReceptor);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEmi, pamRec);
        }
        public async Task ResponderSolicitudAsync(int idEmisor, int idReceptor, string estado)
        {
            string sql = "sp_ResponderSolicitud @id_emisor, @id_receptor, @nuevo_estado";
            SqlParameter pamEmi=new SqlParameter("@id_emisor", idEmisor);
            SqlParameter pamRec= new SqlParameter("@id_receptor", idReceptor);
            SqlParameter pamEst= new SqlParameter("@nuevo_estado", estado);
            await this.context.Database.ExecuteSqlRawAsync(sql,pamEmi,pamRec, pamEst);
        }
        public async Task<InformacionUsuario> PerfilUsuarioAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario user = await consulta.FirstOrDefaultAsync();
            InformacionUsuario usuario = new InformacionUsuario();
            usuario.IdUsuario = user.IdUsuario;
            usuario.Nombre = user.Nombre;
            usuario.Email = user.Email;
            usuario.Corazones = user.CorazonesActuales;
            usuario.Avatar = user.Avatar;
            return usuario;
        }
        public async Task<string> GetEstadoAmistadAsync(int idLogueado, int idPerfil)
        {
            var consulta = from datos in this.context.Amistades
                           where (datos.IdUsuario1 == idLogueado && datos.IdUsuario2 == idPerfil)
                              || (datos.IdUsuario1 == idPerfil && datos.IdUsuario2 == idLogueado)
                           select datos.Estado;

            // ExecuteScalarAsync se traduce aquí en FirstOrDefaultAsync
            // Si no hay fila, devolverá null automáticamente
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetAmistadesAsync(int idUsuario)
        {
            var consulta = from amistad in this.context.Amistades
                           join usuario in this.context.Usuarios
                           on (amistad.IdUsuario1 == idUsuario ? amistad.IdUsuario2 : amistad.IdUsuario1)
                           equals usuario.IdUsuario
                           where (amistad.IdUsuario1 == idUsuario || amistad.IdUsuario2 == idUsuario)
                                 && amistad.Estado == "ACEPTADA"
                           select usuario.Nombre;

            return await consulta.ToListAsync();
        }
        public async Task<List<InformacionUsuario>> BuscarUsuariosNuevosAsync(int idLogueado, string buscar)
        {
            // 1. Obtenemos los IDs de usuarios con los que ya hay amistad (el "NOT IN" del SQL)
            var idsAmigos = await this.context.Amistades
                .Where(a => a.IdUsuario1 == idLogueado || a.IdUsuario2 == idLogueado)
                .Select(a => a.IdUsuario1 == idLogueado ? a.IdUsuario2 : a.IdUsuario1)
                .ToListAsync();

            // 2. Buscamos usuarios cuyo nombre coincida, que no seas tú y que no estén en la lista anterior
            var consulta = from datos in this.context.Usuarios
                           where datos.Nombre.Contains(buscar) // Esto es el LIKE '%buscar%'
                           && datos.IdUsuario != idLogueado
                           && !idsAmigos.Contains(datos.IdUsuario) // El NOT IN
                           select new InformacionUsuario
                           {
                               IdUsuario = datos.IdUsuario,
                               Nombre = datos.Nombre,
                           };

            return await consulta.ToListAsync();
        }
        public async Task<List<UsuarioAmistad>> GetSolicitudesRecibidasAsync(int idLogueado)
        {
            var consulta= from u in this.context.Usuarios
                          join a in this.context.Amistades
                          on u.IdUsuario equals a.IdUsuario1
                          where a.IdUsuario2 == idLogueado && a.Estado=="PENDIENTE"
                          select new UsuarioAmistad
                          {
                              IdUsuario = u.IdUsuario,
                              Nombre = u.Nombre
                          };

            return await consulta.ToListAsync();
            
        }

        public async Task<string> GetRespuestaCorrectaAsync(int idPregunta)
        {
            var consulta = from datos in this.context.Preguntas
                           where datos.IdPregunta == idPregunta
                           select datos.OpcionCorrecta;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<string> FindNombreUsuarioAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos.Nombre;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<SalaJuego> CreateSalaJuegoAsync(int idAnfitrion, int idCuestionario, string tipoJuego, int cantidad, int tiempo, bool publica, int capacidad)
        {
            string codigoSala = Helpers.HelperCreadorSalas.GenerateRoom();

            var consulta = from datos in this.context.Cuestionarios
                           where datos.IdCuestionario == idCuestionario
                           select datos.Titulo;

            string titulo = await consulta.FirstOrDefaultAsync();

            Usuario usuarioAnfitrion = await this.context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idAnfitrion);
            int idPartida;
            using (DbCommand com = this.context.Database.GetDbConnection().CreateCommand())
            {
                com.CommandType = CommandType.StoredProcedure;
                com.CommandText = "sp_CrearPartida";

                com.Parameters.Add(new SqlParameter("@anfitrion_id", idAnfitrion));
                com.Parameters.Add(new SqlParameter("@cuestionario_id", idCuestionario));
                com.Parameters.Add(new SqlParameter("@codigo_sala", codigoSala));
                com.Parameters.Add(new SqlParameter("@tipo_juego", tipoJuego));
                com.Parameters.Add(new SqlParameter("@cantidad_preguntas", cantidad));
                com.Parameters.Add(new SqlParameter("@tiempo_pregunta", tiempo));
                com.Parameters.Add(new SqlParameter("@es_publica", publica));
                com.Parameters.Add(new SqlParameter("@capacidad_maxima", capacidad));

                await com.Connection.OpenAsync();
                idPartida = Convert.ToInt32(await com.ExecuteScalarAsync());
                await com.Connection.CloseAsync();
            }

            // 3. Construcción del objeto de retorno
            SalaJuego sala = new SalaJuego
            {
                IdSala = idPartida,
                CodigoSala = codigoSala,
                IdAnfitrion = idAnfitrion,
                NombreAnfitrion = usuarioAnfitrion.Nombre, 
                AvatarAnfitrion = usuarioAnfitrion.Avatar,
                NombreCuestionario = titulo, 
                Estado = "esperando",
                TipoJuego = tipoJuego,
                CantidadPreguntas = cantidad,
                Tiempo = tiempo,
                Publica = publica,
                CapacidadMaxima= capacidad,
                TotalJugadores = 1,
                Participantes = new List<ParticipantePartida>
                {
                    new ParticipantePartida
                    {
                        IdUsuario = idAnfitrion,
                        Nombre = usuarioAnfitrion.Nombre,
                        Puntuacion = 0,
                        HaRespondido = false
                    }
                }
            };

            return sala;
        }
        public async Task<ParticipantePartida> UnirseAPartidaAsync(int idUsuario, string codigoSala)
        {
            SalaJuego sala = await this.GetSalaPorCodigoAsync(codigoSala);

            // Si la sala no existe, o ya no está esperando, o ya está llena, rebotamos al jugador
            if (sala == null || sala.Estado != "esperando" || sala.TotalJugadores >= sala.CapacidadMaxima)
            {
                return null; // ¡Sala llena o inaccesible!
            }
            // 1. Ejecutamos el SP de acción de forma directa y segura
            string sql = "sp_UnirseAPartida @usuario_id, @codigo_sala";
            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamCod = new SqlParameter("@codigo_sala", codigoSala);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamCod);
            if (sala.TotalJugadores + 1 == sala.CapacidadMaxima)
            {
                // Usamos tu método para cambiar el estado a "jugando"
                await this.CambiarEstadoPartidaAsync(sala.IdSala, "jugando");
            }
            // 2. Obtenemos el nombre y montamos el objeto de retorno
            string nombre = await this.FindNombreUsuarioAsync(idUsuario);

            ParticipantePartida participante = new ParticipantePartida
            {
                IdUsuario = idUsuario,
                Nombre = nombre,
                Puntuacion = 0,
                HaRespondido = false
            };

            return participante;
        }
        public async Task<List<ParticipantePartida>> GetParticipantesSalaAsync(string codigoSala)
        {
            var consulta = from p in this.context.ParticipantesPartida
                           join pr in this.context.Partidas on p.IdPartida equals pr.IdPartida
                           join u in this.context.Usuarios on p.IdUsuario equals u.IdUsuario
                           where pr.CodigoSala == codigoSala 
                           select new ParticipantePartida
                           {
                               IdUsuario = u.IdUsuario,
                               Nombre = u.Nombre,
                               Puntuacion = p.PuntuacionActual,
                               HaRespondido = false
                           };

            return await consulta.ToListAsync();
        }
        
public async Task<SalaJuego> GetSalaPorCodigoAsync(string codigoPartida)
        {
            // 1. Consulta LINQ con Triple JOIN para obtener los datos básicos
            var consulta = from p in this.context.Partidas
                           join u in this.context.Usuarios on p.IdAnfitrion equals u.IdUsuario
                           join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                           where p.CodigoSala == codigoPartida
                           select new SalaJuego
                           {
                               IdSala = p.IdPartida,
                               CodigoSala = p.CodigoSala,
                               Estado = p.Estado,
                               IdAnfitrion = p.IdAnfitrion,
                               NombreAnfitrion = u.Nombre,
                               NombreCuestionario = c.Titulo,
                               TipoJuego = p.TipoJuego,
                               CantidadPreguntas = p.CantidadPreguntas,
                               Publica = p.EsPublica,
                               Tiempo = p.TiempoPregunta,
                               CapacidadMaxima = p.CapacidadMaxima,
                               Participantes = new List<ParticipantePartida>() // Inicializamos la lista
                           };

            // 2. Ejecutamos la consulta
            SalaJuego sala = await consulta.FirstOrDefaultAsync();

            // 3. Si la sala existe, completamos los participantes usando tu otro método
            if (sala != null)
            {
                sala.Participantes = await this.GetParticipantesSalaAsync(sala.CodigoSala);
                sala.TotalJugadores = sala.Participantes.Count;
            }

            return sala;
        }
        public async Task CambiarEstadoPartidaAsync(int idSala, string nuevoEstado)
        {
            
            string sql = "UPDATE partidas SET estado = @estado WHERE partida_id = @id";

            SqlParameter pamEst= new SqlParameter("@estado", nuevoEstado);
            SqlParameter pamId= new SqlParameter("@id", idSala);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEst, pamId);
        }
        public async Task<int> GetIdUsuarioByNombreAsync(string nombre)
        {
            var consulta= from datos in this.context.Usuarios
                          where datos.Nombre == nombre
                          select datos.IdUsuario;
            return await consulta.FirstOrDefaultAsync();
        }


        public async Task RegistrarInvitacionAsync(int idEmisor, int idReceptor, string codigoSala)
        {
            // 1. Buscamos el ID de la partida usando LINQ (equivale al primer SELECT)
            var consultaId = from p in this.context.Partidas
                             where p.CodigoSala == codigoSala
                             select p.IdPartida;

            int idPartida = await consultaId.FirstOrDefaultAsync();

            // 2. Si la partida existe (id > 0), insertamos la invitación
            if (idPartida != 0)
            {
                string sql = @"INSERT INTO invitaciones 
                       (partida_id, usuario_emisor_id, usuario_receptor_id, estado, fecha_invitacion) 
                       VALUES (@idPartida, @idEmi, @idRec, 'pendiente', GETDATE())";

                SqlParameter pamPartida = new SqlParameter("@idPartida", idPartida);
                SqlParameter pamEmi = new SqlParameter("@idEmi", idEmisor);
                SqlParameter pamRec = new SqlParameter("@idRec", idReceptor);

                await this.context.Database.ExecuteSqlRawAsync(sql, pamPartida, pamEmi, pamRec);
            }
        }
        public async Task<List<InvitacionPartida>> GetInvitacionesPendientesAsync(int idReceptor)
        {
            var consulta = from i in this.context.Invitaciones
                           join u in this.context.Usuarios on i.IdEmisor equals u.IdUsuario
                           join p in this.context.Partidas on i.IdPartida equals p.IdPartida
                           join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                           where i.IdReceptor == idReceptor && i.Estado == "pendiente"
                           select new InvitacionPartida
                           {
                               IdInvitacion = i.IdInvitacion,
                               NombreEmisor = u.Nombre,
                               CodigoSala = p.CodigoSala,
                               TituloCuestionario = c.Titulo
                           };

            return await consulta.ToListAsync();
        }
        public async Task ActualizarEstadoInvitacionAsync(int idReceptor, string codigoSala, string nuevoEstado)
        {
            // Buscamos la invitación que coincida con el usuario y la partida (vía su código)
            string sql = @"UPDATE invitaciones 
                   SET estado = @estado 
                   WHERE usuario_receptor_id = @idRec 
                   AND partida_id = (SELECT partida_id FROM partidas WHERE codigo_sala = @cod)";

            
            SqlParameter pamEst=new SqlParameter ("@estado", nuevoEstado);
            SqlParameter pamRec = new SqlParameter("@idRec", idReceptor);
            SqlParameter pamCod = new SqlParameter("@cod", codigoSala);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamEst, pamRec, pamCod);
        }
        public async Task RegistrarRespuestaAsync(int partidaId, int usuarioId, int indicePregunta, bool esCorrecta, int puntos)
        {
            string sql = @"UPDATE PARTICIPANTES_PARTIDA 
                   SET puntuacion_actual = puntuacion_actual + @puntos,
                       indice_respondido = @indice
                   WHERE partida_id = @pid AND usuario_id = @uid";

           
            SqlParameter pamPun=new SqlParameter("@puntos", puntos);
            SqlParameter pamInd = new SqlParameter("@indice", indicePregunta);
            SqlParameter pamPartId = new SqlParameter("@pid", partidaId);
            SqlParameter pamIdU = new SqlParameter("@uid", usuarioId);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamPun, pamInd, pamPartId, pamIdU);
        }
        public async Task<bool> TodosHanRespondidoAsync(int partidaId, int indicePregunta)
        {
            // Obtenemos los participantes de esa partida específica
            var participantes = from datos in this.context.ParticipantesPartida
                                where datos.IdPartida == partidaId
                                select datos;

            // Comprobamos si hay participantes Y si todos cumplen la condición
            // .Any() verifica que la sala no esté vacía
            // .All() verifica que todos tengan un índice igual o mayor al actual
            bool existenParticipantes = await participantes.AnyAsync();
            bool todosListos = await participantes.AllAsync(p => p.IndiceRespondido >= indicePregunta);

            return existenParticipantes && todosListos;
        }

        public async Task<List<RankingJugador>> GetRankingAsync(int partidaId)
        {
            var consulta = from p in this.context.ParticipantesPartida
                           join u in this.context.Usuarios on p.IdUsuario equals u.IdUsuario
                           where p.IdPartida == partidaId
                           orderby p.PuntuacionActual descending // El DESC de SQL
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
            SqlParameter pamInd= new SqlParameter("@indice", nuevoIndice);
            SqlParameter pamPartId = new SqlParameter("@pid", partidaId);
            SqlParameter pamIdUsu = new SqlParameter("@uid", usuarioId);
            await this.context.Database.ExecuteSqlRawAsync(sql,pamInd,pamPartId, pamIdUsu);
        }
        public async Task GuardarHistorialIndividualAsync(int idUsuario, string nombreCuestionario, int puntuacion, int correctas, int totales)
        {
            string sql = "sp_GuardarHistorialIndividual @usuario_id,@nombre_cuestionario,@puntuacion,@preguntas_correctas,@preguntas_totales";
            SqlParameter pamIdUsu= new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamCue = new SqlParameter("@nombre_cuestionario", nombreCuestionario);
            SqlParameter pamPun = new SqlParameter("@puntuacion", puntuacion);
            SqlParameter pamPreC = new SqlParameter("@preguntas_correctas", correctas);
            SqlParameter pamPreT = new SqlParameter("@preguntas_totales", totales);
            await this.context.Database.ExecuteSqlRawAsync(sql,pamIdUsu,pamCue, pamPun, pamPreC,pamPreT);
        }
        public async Task FinalizarPartidaMultijugadorAsync(int idPartida)
        {
            string sql = "sp_FinalizarPartidaMulti @partida_id";
            SqlParameter pamParFin= new SqlParameter("partida_id", idPartida);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamParFin);
        }
        public async Task<List<SalaJuego>> GetSalasPublicasAsync()
        {
            // 1. Atacamos directamente a la vista mapeada en el DbContext
            var consulta = from datos in this.context.SalasPublicas
                           select new SalaJuego
                           {
                               IdSala = datos.IdPartida,
                               CodigoSala = datos.CodigoSala,
                               IdAnfitrion = datos.IdAnfitrion,
                               NombreAnfitrion = datos.NombreAnfitrion,
                               AvatarAnfitrion = datos.AvatarAnfrition,
                               NombreCuestionario = datos.NombreCuestionario,
                               TipoJuego = datos.TipoJuego,
                               CantidadPreguntas = datos.CantidadPreguntas,
                               Publica = datos.EsPublica,
                               TotalJugadores = datos.TotalJugadores,
                               Tiempo = datos.TiempoPregunta,
                               Participantes = new List<ParticipantePartida>()
                           };

            // 2. Ejecutamos y devolvemos la lista en un solo paso
            return await consulta.ToListAsync();
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
            // 1. Obtenemos las partidas finalizadas donde participó el usuario
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

            // 2. Rellenamos los participantes de cada partida
            foreach (var partida in historial)
            {
                // Reutilizamos la lógica de ranking o participantes que ya teníamos
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

                // El ganador es el primero de la lista (ya que ordenamos por puntuación DESC)
                if (partida.Participantes.Any())
                {
                    partida.Ganador = partida.Participantes.First().Nombre;
                }
            }
            return historial;
        }
        public async Task<int> ActualizarYObtenerCorazonesAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                          where datos.IdUsuario == idUsuario
                          select datos;
            Usuario usuario= await consulta.FirstOrDefaultAsync();
            if (usuario == null) return 0;

            // Si ya está al máximo, devolvemos el valor y no hacemos cálculos
            if (usuario.CorazonesActuales >= usuario.CorazonesMaximos)
            {
                return usuario.CorazonesActuales;
            }

            // Si por algún motivo la última recarga es nula, la iniciamos ahora
            if (usuario.UltimaRecarga == null)
            {
                usuario.UltimaRecarga = DateTime.Now;
                await this.context.SaveChangesAsync();
                return usuario.CorazonesActuales;
            }

            int minutosParaRecarga = 30;
            DateTime ahora = DateTime.Now;

            var tiempoTranscurrido = ahora - usuario.UltimaRecarga.Value;
            int corazonesGanados = (int)tiempoTranscurrido.TotalMinutes / minutosParaRecarga;

            if (corazonesGanados > 0)
            {
                int nuevosCorazones = usuario.CorazonesActuales + corazonesGanados;

                if (nuevosCorazones >= usuario.CorazonesMaximos)
                {
                    usuario.CorazonesActuales = usuario.CorazonesMaximos;
                    usuario.UltimaRecarga = ahora; // Reseteamos el cronómetro
                }
                else
                {
                    usuario.CorazonesActuales = nuevosCorazones;
                    // Sumamos solo los bloques de 30 min para no robarle los minutos sobrantes
                    usuario.UltimaRecarga = usuario.UltimaRecarga.Value.AddMinutes(corazonesGanados * minutosParaRecarga);
                }

                await this.context.SaveChangesAsync();
            }

            return usuario.CorazonesActuales;
        }
        public async Task<bool> RecargarPorAnuncioAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();
            if (usuario == null) return false;

            DateTime hoy = DateTime.Now;

            // 1. Reseteamos el contador si es un día nuevo
            if (usuario.FechaUltimoAnuncio == null || usuario.FechaUltimoAnuncio.Value.Date != hoy.Date)
            {
                usuario.AnunciosVistosHoy = 0;
            }

            // 2. Comprobamos el límite diario (ejemplo: 3 anuncios máximo)
            if (usuario.AnunciosVistosHoy >= 3)
            {
                return false;
            }

            // 3. Otorgamos el corazón si no está al máximo
            if (usuario.CorazonesActuales < usuario.CorazonesMaximos)
            {
                usuario.CorazonesActuales++;
                usuario.AnunciosVistosHoy++;
                usuario.FechaUltimoAnuncio = hoy;

                // Nota: NO tocamos usuario.UltimaRecarga. 
                // El cronómetro pasivo sigue corriendo por detrás.

                await this.context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        public async Task<bool> ConsumirCorazonAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            Usuario usuario = await consulta.FirstOrDefaultAsync();
            if (usuario == null || usuario.CorazonesActuales <= 0)
            {
                return false;
            }

            // Si los corazones estaban al máximo, el cronómetro de recarga debe empezar JUSTO AHORA
            if (usuario.CorazonesActuales == usuario.CorazonesMaximos)
            {
                usuario.UltimaRecarga = DateTime.Now;
            }

            usuario.CorazonesActuales--;
            await this.context.SaveChangesAsync();

            return true;
        }
        public async Task GuardarRankingModoAsync(int idUsuario, string modoJuego, int puntos, string nombreCuestionario)
        {
            string sql = "sp_GuardarRankingModo @usuario_id, @modo_juego, @puntos,@nombre_cuestionario";

            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamModo = new SqlParameter("@modo_juego", modoJuego); 
            SqlParameter pamPuntos = new SqlParameter("@puntos", puntos);
            SqlParameter pamCuestionario = new SqlParameter("@nombre_cuestionario", nombreCuestionario);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamModo, pamPuntos,pamCuestionario);
        }
       
        public async Task EnviarReporteAsync(int idPregunta, int idUsuario, string motivo, string comentario)
        {
            string sql = @"
                INSERT INTO REPORTES_PREGUNTAS 
                (pregunta_id, usuario_id, motivo_reporte, comentario_adicional, fecha_reporte, estado_reporte)
                VALUES 
                (@idPregunta, @idUsuario, @motivo, @comentario, GETDATE(), 'ABIERTO')";

            SqlParameter pamPregunta = new SqlParameter("@idPregunta", idPregunta);
            SqlParameter pamUsuario = new SqlParameter("@idUsuario", idUsuario);
            SqlParameter pamMotivo = new SqlParameter("@motivo", string.IsNullOrEmpty(motivo) ? (object)DBNull.Value : motivo);
            SqlParameter pamComentario = new SqlParameter("@comentario", string.IsNullOrEmpty(comentario) ? (object)DBNull.Value : comentario);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamPregunta, pamUsuario, pamMotivo, pamComentario);
        }
        public async Task<List<VistaReportePregunta>> GetReportesAbiertosAsync()
        {
            var consulta = from r in this.context.ReportePreguntas
                           join p in this.context.Preguntas
                           on r.IdPregunta equals p.IdPregunta 
                           join u in this.context.Usuarios
                           on r.IdUsuario equals u.IdUsuario   
                           where r.EstadoReporte == "ABIERTO"
                           orderby r.FechaReporte ascending
                           select new VistaReportePregunta
                           {
                               IdReporte = r.IdReporte,
                               IdPregunta = r.IdPregunta,
                               EnunciadoPregunta = p.Enunciado,
                               NombreUsuario = u.Nombre, 
                               Motivo = r.MotivoReporte,
                               Comentario = r.ComentarioAdicional,
                               Fecha = r.FechaReporte,
                               Estado = r.EstadoReporte
                           };

            return await consulta.ToListAsync();
        }
        public async Task ResolverReporteAsync(int idReporte)
        {
            string sql = "UPDATE REPORTES_PREGUNTAS SET estado_reporte = 'CERRADO' WHERE reporte_id = @id";
            SqlParameter pamId = new SqlParameter("@id", idReporte);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamId);
        }
        
        public async Task<List<int>> GetIdsPreguntasAsync(string nombreCuestionario, int? nivel)
        {
            
            if (string.IsNullOrEmpty(nombreCuestionario) || nombreCuestionario == "MIXTO")
            {
                var consultaMixta = from p in this.context.Preguntas
                                    join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                                    where c.CreadorId == 1
                                    select p.IdPregunta;
                return await consultaMixta.ToListAsync();
            }

            
            var consulta = from p in this.context.Preguntas
                           join c in this.context.Cuestionarios on p.IdCuestionario equals c.IdCuestionario
                           where c.Titulo.Trim().ToLower() == nombreCuestionario.Trim().ToLower()
                           select p;

            
            if (nivel.HasValue && nivel.Value > 0)
            {
                consulta = consulta.Where(p => p.Nivel == nivel.Value);
            }

            
            return await consulta.Select(p => p.IdPregunta).ToListAsync();
        }

        
        public async Task<Pregunta> GetPreguntaByIdAsync(int idPregunta)
        {
            var consulta = from datos in this.context.Preguntas
                           where datos.IdPregunta == idPregunta
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }
        public async Task<List<RankingModo>> GetRankingsPorUsuarioAsync(int idUsuario)
        {
            var consulta = from r in this.context.RankingModos
                           join c in this.context.Cuestionarios
                           on r.IdCuestionario equals c.IdCuestionario // <-- Usa el nombre de la PK de tu clase Cuestionario
                           where r.IdUsuario == idUsuario
                           orderby r.Puntos descending
                           select new RankingModo
                           {
                               IdUsuario = r.IdUsuario,
                               ModoJuego = r.ModoJuego,
                               IdCuestionario = r.IdCuestionario,
                               Puntos = r.Puntos,
                               FechaPartida = r.FechaPartida,
                               // Llenamos la propiedad "fantasma" con el nombre real
                               NombreCuestionario = c.Titulo // <-- Cambia 'Nombre' por 'Titulo' o como se llame la propiedad en tu clase Cuestionario
                           };

            return await consulta.ToListAsync();
        }
        

       
        public async Task DeleteUsuario(int idUsuario)
        {
            string sql = " sp_EliminarUsuario @usuario_id";
            SqlParameter pamIdUsuario = new SqlParameter("@usuario_id", idUsuario);
            await this.context.Database.ExecuteSqlRawAsync(sql, pamIdUsuario);
        }
        public async Task RegistrarPagoYActivarVipAsync(int idUsuario, string sessionId, string tipoPlan)
        {
            // 1. Definir precio según el plan
            decimal monto = (tipoPlan == "anual") ? 9.99m : 0.99m;

            
            Pago pago = new Pago
            {
                IdUsuario = idUsuario,
                StripeSessionId = sessionId,
                Monto = monto,
                PlanTipo = tipoPlan,
                FechaPago = DateTime.Now
            };

            this.context.Pagos.Add(pago);

            // 3. Buscar usuario y darle poderes VIP
            var user = await this.context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (user != null)
            {
                user.RolId = 2; // Rol PREMIUM
                user.CorazonesActuales = 999; // Corazones infinitos
            }

            await this.context.SaveChangesAsync();
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
                               nombreCategoria = cat.Nombre // Aquí obtenemos el nombre real (ej: "Historia")
                           };

            // Aplicamos el filtro si el usuario eligió una categoría en el select
            if (!string.IsNullOrEmpty(filtro))
            {
                // Comparamos el nombre de la categoría con el filtro que llega de la vista
                consulta = consulta.Where(r => r.nombreCategoria == filtro);
            }

            // Ordenación
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
        public async Task<Usuario> GenerarTokenRecuperacionAsync(string email)
        {
            
            Usuario usuario = await this.context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            
            if (usuario == null) return null;

            
            usuario.TokenMail = Guid.NewGuid().ToString();

            await this.context.SaveChangesAsync();

            return usuario;
        }
        public async Task<bool> ResetPasswordAsync(string token, string passwordNormal, string passwordHash, string salt)
        {
            string sql = "sp_CambiarPassword @usuario_id, @nuevo_password, @nuevo_hash, @nuevo_salt";

            Usuario usuario = await this.context.Usuarios.FirstOrDefaultAsync(u => u.TokenMail == token);

            if (usuario == null) return false; 

            SqlParameter pamId = new SqlParameter("@usuario_id", usuario.IdUsuario);
            SqlParameter pamPass = new SqlParameter("@nuevo_password", passwordNormal);
            SqlParameter pamHash = new SqlParameter("@nuevo_hash", passwordHash);
            SqlParameter pamSalt = new SqlParameter("@nuevo_salt", salt);
            
            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamPass, pamHash, pamSalt);

            return true;
        }
        public async Task<string> ComprobarUsuarioDuplicadoAsync(string nombre, string email)
        {
            // Comprobamos primero el correo (suele ser lo más crítico)
            bool existeEmail = await this.context.Usuarios.AnyAsync(u => u.Email == email);
            if (existeEmail) return "email";

            // Luego comprobamos el nombre
            bool existeNombre = await this.context.Usuarios.AnyAsync(u => u.Nombre == nombre);
            if (existeNombre) return "nombre";

            // Si llega aquí, es que todo está libre
            return null;
        }

        public async Task CambiarPasswordDesdePerfilAsync(int idUsuario, string passwordNormal, string passwordHash, string salt)
        {
            string sql = "sp_CambiarPassword @usuario_id, @nuevo_password, @nuevo_hash, @nuevo_salt";

            SqlParameter pamId = new SqlParameter("@usuario_id", idUsuario);
            SqlParameter pamPass = new SqlParameter("@nuevo_password", passwordNormal);
            SqlParameter pamHash = new SqlParameter("@nuevo_hash", passwordHash);
            SqlParameter pamSalt = new SqlParameter("@nuevo_salt", salt);

            await this.context.Database.ExecuteSqlRawAsync(sql, pamId, pamPass, pamHash, pamSalt);
        }
        public async Task<int> GetNumeroSolicitudesPendientesAsync(int idLogueado)
        {
            
            return await this.context.Amistades
                .CountAsync(a => a.IdUsuario2 == idLogueado && a.Estado == "PENDIENTE");
        }
        public async Task<List<Usuario>> BuscarUsuariosPorNombreAsync(string nombreBusqueda, int idLogueado)
        {
            
            return await this.context.Usuarios
                .Where(u => u.Nombre.Contains(nombreBusqueda) && u.IdUsuario != idLogueado)
                .Take(10) 
                .ToListAsync();
        }
        public async Task<bool> CancelarSalaAnfitrionAsync(int idSala, int idAnfitrion)
        {
            
            string sql = @"UPDATE partidas 
                   SET estado = 'finalizada' 
                   WHERE partida_id = @idSala 
                   AND anfitrion_id = @idAnfitrion 
                   AND estado = 'esperando'";

            Microsoft.Data.SqlClient.SqlParameter pamSala = new Microsoft.Data.SqlClient.SqlParameter("@idSala", idSala);
            Microsoft.Data.SqlClient.SqlParameter pamAnf = new Microsoft.Data.SqlClient.SqlParameter("@idAnfitrion", idAnfitrion);

           
            int filasAfectadas = await this.context.Database.ExecuteSqlRawAsync(sql, pamSala, pamAnf);

            
            return filasAfectadas > 0;
        }
    }
}
