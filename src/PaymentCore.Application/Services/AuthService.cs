using PaymentCore.Application.DTOs;
using PaymentCore.Application.Interfaces;
using PaymentCore.Domain.Entities;
using PaymentCore.Domain.Interfaces;

namespace PaymentCore.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Validar se username já existe
        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            throw new InvalidOperationException($"Username '{request.Username}' já está em uso");
        }

        // Validar se email já existe
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new InvalidOperationException($"Email '{request.Email}' já está em uso");
        }

        // Criar hash da senha
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Criar usuário
        var user = new User(request.Username, request.Email, passwordHash, request.Role);

        await _userRepository.CreateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Gerar token
        var token = _tokenService.GenerateToken(user.Id, user.Username, user.Email, user.Role);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Buscar usuário por username ou email
        User? user = null;

        if (request.UsernameOrEmail.Contains('@'))
        {
            user = await _userRepository.GetByEmailAsync(request.UsernameOrEmail, cancellationToken);
        }
        else
        {
            user = await _userRepository.GetByUsernameAsync(request.UsernameOrEmail, cancellationToken);
        }

        if (user == null)
        {
            throw new UnauthorizedAccessException("Credenciais inválidas");
        }

        // Verificar senha
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciais inválidas");
        }

        // Verificar se usuário está ativo
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Usuário desativado");
        }

        // Atualizar último login
        user.UpdateLastLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Gerar token
        var token = _tokenService.GenerateToken(user.Id, user.Username, user.Email, user.Role);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        };
    }
}
