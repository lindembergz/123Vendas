using FluentValidation;
using Venda.Application.Commands;

namespace Venda.Application.Validators;

public class AtualizarVendaValidator : AbstractValidator<AtualizarVendaCommand>
{
    public AtualizarVendaValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId é obrigatório");
        
        RuleFor(x => x.VendaId)
            .NotEmpty().WithMessage("VendaId é obrigatório");
        
        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("Venda deve conter ao menos um item")
            .Must(itens => itens.Count <= 100).WithMessage("Venda não pode ter mais de 100 itens");
        
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
