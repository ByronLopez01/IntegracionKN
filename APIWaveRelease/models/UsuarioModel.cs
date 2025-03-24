using System.ComponentModel.DataAnnotations;

namespace APIWaveRelease.models
{
    public class UsuarioModel
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Contrasena { get; set; }
    }
}