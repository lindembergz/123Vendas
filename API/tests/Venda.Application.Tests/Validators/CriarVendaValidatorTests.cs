using FluentAssertions;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Validators;
using Xunit;

namespace Venda.Application.Tests.Validators;

public class CriarVendaValidatorTests
{
    private readonly CriarVendaValidator _validator;

    public CriarVendaValidatorTests()
    {
        _validator = new CriarVendaValidator();
    }

    [Fact]
    public void Validacao_ComDadosValidos_DevePassar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validacao_ComRequestIdVazio_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.Empty,
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "RequestId");
        result.Errors.First(e => e.PropertyName == "RequestId")
            .ErrorMessage.Should().Be("RequestId é obrigatório");
    }

    [Fact]
    public void Validacao_ComClienteIdVazio_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.Empty,
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ClienteId");
        result.Errors.First(e => e.PropertyName == "ClienteId")
            .ErrorMessage.Should().Be("ClienteId é obrigatório");
    }

    [Fact]
    public void Validacao_ComFilialIdVazia_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.Empty,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "FilialId");
        result.Errors.First(e => e.PropertyName == "FilialId")
            .ErrorMessage.Should().Be("FilialId é obrigatória");
    }

    [Fact]
    public void Validacao_ComListaDeItensVazia_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens");
        result.Errors.First(e => e.PropertyName == "Itens")
            .ErrorMessage.Should().Be("Venda deve conter ao menos um item");
    }

    [Fact]
    public void Validacao_ComMaisDe100Itens_DeveFalhar()
    {
        // Arrange
        var itens = Enumerable.Range(1, 101)
            .Select(_ => new ItemVendaDto(Guid.NewGuid(), 1, 100m, 0m, 100m))
            .ToList();

        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: itens
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens");
        result.Errors.First(e => e.PropertyName == "Itens")
            .ErrorMessage.Should().Be("Venda não pode ter mais de 100 itens");
    }

    [Fact]
    public void Validacao_ComItemSemProdutoId_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.Empty, 2, 100m, 0m, 200m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens[0].ProdutoId");
        result.Errors.First(e => e.PropertyName == "Itens[0].ProdutoId")
            .ErrorMessage.Should().Be("ProdutoId é obrigatório");
    }

    [Fact]
    public void Validacao_ComQuantidadeZero_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 0, 100m, 0m, 0m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens[0].Quantidade");
        result.Errors.First(e => e.PropertyName == "Itens[0].Quantidade")
            .ErrorMessage.Should().Be("Quantidade deve ser maior que zero");
    }

    [Fact]
    public void Validacao_ComValorUnitarioZero_DeveFalhar()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 0m, 0m, 0m)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens[0].ValorUnitario");
        result.Errors.First(e => e.PropertyName == "Itens[0].ValorUnitario")
            .ErrorMessage.Should().Be("Valor unitário deve ser maior que zero");
    }

    [Fact]
    public void Validacao_ComMultiplosErros_DeveRetornarTodos()
    {
        // Arrange
        var command = new CriarVendaCommand(
            RequestId: Guid.Empty,
            ClienteId: Guid.Empty,
            FilialId: Guid.Empty,
            Itens: new List<ItemVendaDto>()
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(4);
        result.Errors.Should().Contain(e => e.PropertyName == "RequestId");
        result.Errors.Should().Contain(e => e.PropertyName == "ClienteId");
        result.Errors.Should().Contain(e => e.PropertyName == "FilialId");
        result.Errors.Should().Contain(e => e.PropertyName == "Itens");
    }
}
