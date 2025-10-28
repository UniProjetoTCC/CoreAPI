using System;
using System.Collections.Generic;
using Business.Services.Base;

namespace CoreAPI.Models
{
    /// <summary>
    /// Modelo para resposta básica de healthcheck
    /// </summary>
    public class HealthCheckResponse
    {
        /// <summary>
        /// Status da requisição
        /// </summary>
        public string Status { get; set; } = "healthy";

        /// <summary>
        /// Timestamp da verificação
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Modelo para resposta do endpoint healthcheck/database
    /// </summary>
    public class DatabaseHealthResponse : HealthCheckResponse
    {
        /// <summary>
        /// Construtor padrão
        /// </summary>
        public DatabaseHealthResponse()
        {
            Status = "Database Connection Successful";
        }
    }

    /// <summary>
    /// Modelo para resposta do endpoint healthcheck/user
    /// </summary>
    public class HealthCheckUserResponse
    {
        /// <summary>
        /// Status da requisição
        /// </summary>
        public string Status { get; set; } = "Success";

        /// <summary>
        /// Informações do usuário
        /// </summary>
        public UserHealthInfo User { get; set; } = new();
    }

    /// <summary>
    /// Informações de saúde do usuário
    /// </summary>
    public class UserHealthInfo
    {
        /// <summary>
        /// Email do usuário
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Nome de usuário
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// ID do usuário
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Papéis/roles do usuário
        /// </summary>
        public IEnumerable<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Indica se o usuário é um usuário vinculado
        /// </summary>
        public bool IsLinkedUser { get; set; }

        /// <summary>
        /// Detalhes do usuário vinculado, se aplicável
        /// </summary>
        public LinkedUserHealthInfo? LinkedUserDetails { get; set; }
    }

    /// <summary>
    /// Informações de saúde do usuário vinculado
    /// </summary>
    public class LinkedUserHealthInfo
    {
        /// <summary>
        /// ID do usuário vinculado
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// ID do usuário pai/criador
        /// </summary>
        public string? ParentUserId { get; set; }

        /// <summary>
        /// ID do grupo ao qual o usuário pertence
        /// </summary>
        public string? GroupId { get; set; }

        /// <summary>
        /// Indica se o usuário vinculado está ativo
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Permissões do usuário vinculado
        /// </summary>
        public LinkedUserPermissions Permissions { get; set; } = new();
    }

    /// <summary>
    /// Modelo para respostas de erro
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Status da requisição
        /// </summary>
        public string Status { get; set; } = "Error";

        /// <summary>
        /// Mensagem de erro
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp do erro
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
