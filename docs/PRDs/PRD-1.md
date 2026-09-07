# PRD — Reservas para retirada

## Ideia do produto

Uma pequena loja precisa registrar os itens que separou para clientes. Hoje, o atendente anota essas reservas em papel e acaba esquecendo de liberar itens que não foram retirados.

O produto permite registrar uma reserva, confirmar a retirada e encerrar automaticamente as reservas que passaram do prazo.

Este é um projeto de treino. O primeiro recorte tem apenas um subdomínio: **Reservas**.

## Quem usa

O atendente da loja, que separa o item e acompanha se o cliente veio buscá-lo.

## O que entra na primeira versão

- Registrar uma reserva com nome do cliente, descrição do item e quantidade.
- Consultar uma reserva e listar as reservas abertas.
- Confirmar que o cliente retirou os itens.
- Cancelar uma reserva.
- Encerrar automaticamente uma reserva quando o prazo acabar.

## Regras do negócio

1. Uma reserva contém apenas um tipo de item e uma quantidade maior que zero.
2. O nome do cliente e a descrição do item são obrigatórios.
3. Toda reserva começa **Aberta** e vale por 15 minutos.
4. Antes do vencimento, o atendente pode marcar a reserva como **Retirada** ou **Cancelada**.
5. Ao atingir o prazo, a reserva não pode mais ser retirada ou cancelada e deve passar automaticamente para **Expirada**.
6. Uma reserva Retirada, Cancelada ou Expirada está encerrada e não pode ser alterada.

## Exemplo

Ana pede que a loja separe dois cadernos. Às 14h, o atendente registra a reserva em seu nome.

Se Ana buscar os cadernos antes das 14h15, o atendente confirma a retirada. Se ela desistir antes desse horário, ele cancela. Caso contrário, a reserva expira e o atendente sabe que pode devolver os cadernos à prateleira.

## O que fica de fora

Não haverá controle de estoque, cadastro de produtos ou clientes, pagamento, notificações ou tela. O atendente verifica a disponibilidade e separa os itens fisicamente; o produto acompanha apenas a reserva.

## Quando estará pronto

Será possível registrar e consultar uma reserva, confirmar uma retirada, cancelar e observar uma reserva expirar automaticamente. Uma reserva encerrada não poderá mudar de situação.

## Formato do treino

Uma API recebe as ações do atendente e permite as consultas. Um Worker encerra as reservas vencidas. Sem frontend.

Você pode implementar esse mesmo escopo primeiro em Clean Architecture e depois em outro repositório com VSA.

Depois que essa versão estiver pronta, o próximo exercício pode ser permitir a prorrogação do prazo — ainda dentro do subdomínio de Reservas.
