using _123Vendas.Shared.Common;
using FluentAssertions;

namespace Shared.Tests;

public class ResultTests
{
    [Fact]
    public void Success_DeveRetornarIsSuccessTrue()
    {
        
        var result = Result.Success();
        
        
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }
    
    [Fact]
    public void Failure_DeveRetornarIsFailureTrueEErrorPreenchido()
    {
        
        var errorMessage = "Erro de teste";
        
        
        var result = Result.Failure(errorMessage);
        
        
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }
    
    [Fact]
    public void GenericSuccess_DeveRetornarValueCorretamente()
    {
        
        var expectedValue = 42;
        
        
        var result = Result<int>.Success(expectedValue);
        
        
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(expectedValue);
        result.Error.Should().BeNull();
    }
    
    [Fact]
    public void GenericFailure_DeveRetornarErroSemValue()
    {
        
        var errorMessage = "Erro genérico de teste";
        
        
        var result = Result<int>.Failure(errorMessage);
        
        
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.Value.Should().Be(default(int));
    }
    
    [Fact]
    public void GenericSuccess_ComObjetoComplexo_DeveRetornarValueCorretamente()
    {
        
        var expectedValue = new TestObject { Id = 1, Name = "Test" };
        
        
        var result = Result<TestObject>.Success(expectedValue);
        
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(expectedValue);
    }
    
    [Fact]
    public void GenericFailure_ComObjetoComplexo_DeveRetornarValueNull()
    {
        
        var errorMessage = "Erro ao processar objeto";
        
        
        var result = Result<TestObject>.Failure(errorMessage);
        
        
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().Be(errorMessage);
    }
    
    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
