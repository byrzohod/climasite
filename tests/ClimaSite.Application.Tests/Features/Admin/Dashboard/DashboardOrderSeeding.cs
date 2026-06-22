using ClimaSite.Core.Entities;

namespace ClimaSite.Application.Tests.Features.Admin.Dashboard;

/// <summary>
/// Shared helpers for Dashboard query-handler tests. Order status transitions are validated by the
/// domain (e.g. Pending can only move to Paid/Cancelled/PaymentFailed), so reaching Processing,
/// Shipped or Delivered requires walking the full valid path rather than a single jump.
/// </summary>
internal static class DashboardOrderSeeding
{
    public static void WalkToStatus(Order order, OrderStatus? target)
    {
        if (!target.HasValue || target.Value == OrderStatus.Pending)
        {
            return;
        }

        switch (target.Value)
        {
            case OrderStatus.Paid:
                order.SetStatus(OrderStatus.Paid);
                break;
            case OrderStatus.Processing:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                break;
            case OrderStatus.Shipped:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                break;
            case OrderStatus.Delivered:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                order.SetStatus(OrderStatus.Delivered);
                break;
            case OrderStatus.Cancelled:
                order.SetStatus(OrderStatus.Cancelled);
                break;
            case OrderStatus.PaymentFailed:
                order.SetStatus(OrderStatus.PaymentFailed);
                break;
            case OrderStatus.Refunded:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Refunded);
                break;
            case OrderStatus.Returned:
                order.SetStatus(OrderStatus.Paid);
                order.SetStatus(OrderStatus.Processing);
                order.SetStatus(OrderStatus.Shipped);
                order.SetStatus(OrderStatus.Returned);
                break;
            default:
                order.SetStatus(target.Value);
                break;
        }
    }

    /// <summary>
    /// The OrderItem.Order navigation is not wired by MockDbContext; queries that read
    /// oi.Order.CreatedAt need it set the way EF would materialise it via Include.
    /// </summary>
    public static void LinkOrderNavigation(Order order)
    {
        var orderProperty = typeof(OrderItem).GetProperty("Order")!;
        foreach (var item in order.Items)
        {
            orderProperty.SetValue(item, order);
        }
    }

    /// <summary>Sets the Order.User navigation that GetRecentOrders reads via Include.</summary>
    public static void LinkUserNavigation(Order order, ApplicationUser user)
    {
        typeof(Order).GetProperty("User")!.SetValue(order, user);
    }
}
