using Microsoft.EntityFrameworkCore;

using ProyectoPersonal.Models;

namespace ProyectoPersonal.Data
{
    public class TrivialContext: DbContext
    {
        public TrivialContext(DbContextOptions<TrivialContext> options)
            :base(options) { }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Login> Logins { get; set; }
        public DbSet<Cuestionario> Cuestionarios { get; set; }
        public DbSet<Pregunta> Preguntas { get; set; }
        public DbSet<HistorialIndividualPartidas> HistorialIndividual { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<ReportePregunta> ReportePreguntas { get; set; }
        public DbSet<Partida> Partidas { get; set; }
        public DbSet<ParticipanteEntidad> ParticipantesPartida { get; set; }
        public DbSet<Amistad> Amistades { get; set; }
        public DbSet<Invitacion> Invitaciones { get; set; }
        public DbSet<VistaSalaPublica> SalasPublicas { get; set; }
        public DbSet<RankingModo> RankingModos { get; set; }
        
        public DbSet<Pago> Pagos { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Amistad>()
                .HasKey(a => new { a.IdUsuario1, a.IdUsuario2 });

            modelBuilder.Entity<ParticipanteEntidad>()
                .HasKey(p => new { p.IdPartida, p.IdUsuario });
            
            modelBuilder.Entity<HistorialIndividualPartidas>()
                .HasNoKey();
            modelBuilder.Entity<VistaSalaPublica>().HasNoKey();
        }
    }
}
