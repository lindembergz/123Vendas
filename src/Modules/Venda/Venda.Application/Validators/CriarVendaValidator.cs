using FluentValidation;
using Venda.Application.Commands;

namespace Venda.Application.Validators;

public class CriarVendaValidator : AbstractValidator<CriarVendaCommand>
{
    public CriarVendaValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId é obrigatório");
        
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("ClienteId é obrigatório");
        
        RuleFor(x => x.FilialId)
            .NotEmpty().WithMessage("FilialId é obrigatória");
        
        RuleFor(x => x.Itens)
            .NotNull().WithMessage("Itens é obrigatório")
            .NotEmpty().WithMessage("Venda deve conter ao menos um item")
            .Must(itens => itens == null || itens.Count <= 100).WithMessage("Venda não pode ter mais de 100 itens");
        
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId)
                .NotEmpty().WithMessage("ProdutoId é obrigatório");
            
            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero");
            
            item.RuleFor(i => i.ValorUnitario)
                .GreaterThan(0).WithMessage("Valor unitário deve ser maior que zero");
        });
    }
}
