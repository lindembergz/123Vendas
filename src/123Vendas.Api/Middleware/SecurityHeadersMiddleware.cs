namespace _123Vendas.Api.Middleware;

/// <summary>
/// Middleware que adiciona headers de segurança HTTP em todas as respostas.
/// Protege contra ataques comuns: XSS, Clickjacking, MIME sniffing, etc.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Adiciona headers de segurança antes de processar a requisição
        AddSecurityHeaders(context);

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // X-Content-Type-Options: Previne MIME sniffing
        // Força o browser a respeitar o Content-Type declarado
        headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options: Previne Clickjacking
        // Impede que a página seja carregada em iframes
        headers["X-Frame-Options"] = "DENY";

        // X-XSS-Protection: Proteção XSS legada (browsers antigos)
        // Modo 1 = ativa filtro XSS, mode=block = bloqueia página inteira
        headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy: Controla informações de referrer enviadas
        // strict-origin-when-cross-origin = envia origin apenas em HTTPS cross-origin
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions-Policy: Controla acesso a APIs do browser
        // Desabilita recursos sensíveis que a API não precisa
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=()";

        // Content-Security-Policy: Previne XSS e injeção de código
        // Política restritiva para API (não serve HTML/JS normalmente)
        var csp = _environment.IsDevelopment()
            ? BuildDevelopmentCSP()
            : BuildProductionCSP();
        headers["Content-Security-Policy"] = csp;

        // Strict-Transport-Security: Força HTTPS (apenas em produção)
        // max-age=31536000 = 1 ano, includeSubDomains = aplica a subdomínios
        if (!_environment.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Remove headers que expõem informações do servidor
        headers.Remove("Server");
        headers.Remove("X-Powered-By");
        headers.Remove("X-AspNet-Version");
        headers.Remove("X-AspNetMvc-Version");
    }

    /// <summary>
    /// CSP para desenvolvimento: permite Swagger UI funcionar
    /// </summary>
    private static string BuildDevelopmentCSP()
    {
        return string.Join("; ", new[]
        {
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // Swagger precisa de inline/eval
            "style-src 'self' 'unsafe-inline'", // Swagger precisa de inline styles
            "img-src 'self' data: https:", // Swagger usa data URIs
            "font-src 'self' data:",
            "connect-src 'self'",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'"
        });
    }

    /// <summary>
    /// CSP para produção: política restritiva (API pura, sem UI)
    /// </summary>
    private static string BuildProductionCSP()
    {
        return string.Join("; ", new[]
        {
            "default-src 'none'", // Bloqueia tudo por padrão
            "frame-ancestors 'none'", // Não pode ser embedado
            "base-uri 'self'",
            "form-action 'self'"
        });
    }
}
