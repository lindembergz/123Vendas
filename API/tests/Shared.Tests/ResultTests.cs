using _123Vendas.Shared.Common;
using FluentAssertions;

namespace Shared.Tests;

public class ResultTests
{
    [Fact]
    public void Success_DeveRetornarIsSuccessTrue()
    {
        // Act
        var result = Result.Success();
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }
    
    [Fact]
    public void Failure_DeveRetornarIsFailureTrueEErrorPreenchido()
    {
        // Arrange
        var errorMessage = "Erro de teste";
        
        // Act
        var result = Result.Failure(errorMessage);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }
    
    [Fact]
    public void GenericSuccess_DeveRetornarValueCorretamente()
    {
        // Arrange
        var expectedValue = 42;
        
        // Act
        var result = Result<int>.Success(expectedValue);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(expectedValue);
        result.Error.Should().BeNull();
    }
    
    [Fact]
    public void GenericFailure_DeveRetornarErroSemValue()
    {
        // Arrange
        var errorMessage = "Erro genérico de teste";
        
        // Act
        var result = Result<int>.Failure(errorMessage);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
        result.Value.Should().Be(default(int));
    }
    
    [Fact]
    public void GenericSuccess_ComObjetoComplexo_DeveRetornarValueCorretamente()
    {
        // Arrange
        var expectedValue = new TestObject { Id = 1, Name = "Test" };
        
        // Act
        var result = Result<TestObject>.Success(expectedValue);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(expectedValue);
    }
    
    [Fact]
    public void GenericFailure_ComObjetoComplexo_DeveRetornarValueNull()
    {
        // Arrange
        var errorMessage = "Erro ao processar objeto";
        
        // Act
        var result = Result<TestObject>.Failure(errorMessage);
        
        // Assert
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
