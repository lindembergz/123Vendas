using FluentAssertions;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Validators;
using Xunit;

namespace Venda.Application.Tests.Validators;

public class AtualizarVendaValidatorTests
{
    private readonly AtualizarVendaValidator _validator;

    public AtualizarVendaValidatorTests()
    {
        _validator = new AtualizarVendaValidator();
    }

    [Fact]
    public void Validacao_ComDadosValidos_DevePassar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 3, 150m, 0m, 450m)
            }
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validacao_ComRequestIdVazio_DeveFalhar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.Empty,
            VendaId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 3, 150m, 0m, 450m)
            }
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "RequestId");
        result.Errors.First(e => e.PropertyName == "RequestId")
            .ErrorMessage.Should().Be("RequestId é obrigatório");
    }

    [Fact]
    public void Validacao_ComVendaIdVazio_DeveFalhar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.Empty,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 3, 150m, 0m, 450m)
            }
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "VendaId");
        result.Errors.First(e => e.PropertyName == "VendaId")
            .ErrorMessage.Should().Be("VendaId é obrigatório");
    }

    [Fact]
    public void Validacao_ComListaDeItensVazia_DeveFalhar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>()
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens");
        result.Errors.First(e => e.PropertyName == "Itens")
            .ErrorMessage.Should().Be("Venda deve conter ao menos um item");
    }

    [Fact]
    public void Validacao_ComMaisDe100Itens_DeveFalhar()
    {
        
        var itens = Enumerable.Range(1, 101)
            .Select(_ => new ItemVendaDto(Guid.NewGuid(), 1, 100m, 0m, 100m))
            .ToList();

        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.NewGuid(),
            Itens: itens
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens");
        result.Errors.First(e => e.PropertyName == "Itens")
            .ErrorMessage.Should().Be("Venda não pode ter mais de 100 itens");
    }

    [Fact]
    public void Validacao_ComItemSemProdutoId_DeveFalhar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.Empty, 3, 150m, 0m, 450m)
            }
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens[0].ProdutoId");
        result.Errors.First(e => e.PropertyName == "Itens[0].ProdutoId")
            .ErrorMessage.Should().Be("ProdutoId é obrigatório");
    }

    [Fact]
    public void Validacao_ComQuantidadeZero_DeveFalhar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 0, 150m, 0m, 0m)
            }
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens[0].Quantidade");
        result.Errors.First(e => e.PropertyName == "Itens[0].Quantidade")
            .ErrorMessage.Should().Be("Quantidade deve ser maior que zero");
    }

    [Fact]
    public void Validacao_ComValorUnitarioZero_DeveFalhar()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: Guid.NewGuid(),
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 3, 0m, 0m, 0m)
            }
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Itens[0].ValorUnitario");
        result.Errors.First(e => e.PropertyName == "Itens[0].ValorUnitario")
            .ErrorMessage.Should().Be("Valor unitário deve ser maior que zero");
    }

    [Fact]
    public void Validacao_ComMultiplosErros_DeveRetornarTodos()
    {
        
        var command = new AtualizarVendaCommand(
            RequestId: Guid.Empty,
            VendaId: Guid.Empty,
            Itens: new List<ItemVendaDto>()
        );

        
        var result = _validator.Validate(command);

        
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.PropertyName == "RequestId");
        result.Errors.Should().Contain(e => e.PropertyName == "VendaId");
        result.Errors.Should().Contain(e => e.PropertyName == "Itens");
    }
}
