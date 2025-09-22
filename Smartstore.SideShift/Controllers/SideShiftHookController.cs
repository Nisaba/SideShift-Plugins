using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.SideShift.Models;
using Smartstore.SideShift.Providers;
using Smartstore.SideShift.Settings;
using Smartstore.Web.Controllers;

namespace Smartstore.SideShift.Controllers
{
    public class SideShiftHookController : PublicController
    {
        private readonly ILogger _logger;
        private readonly SmartDbContext _db;
        private readonly SideShiftSettings _settings;

        public SideShiftHookController(IOrderService orderService,
            SideShiftSettings settings,
            SmartDbContext db,
           ICommonServices services,
            ILogger logger)
        {
            _logger = logger;
            _db = db;
            _settings = settings;
        }

        [HttpPost]
        public async Task<IActionResult> Process([FromBody] SideShiftWebhook payload)
        {
            try
            {                
                var order = await _db.Orders.FirstOrDefaultAsync(x =>
                    x.PaymentMethodSystemName == PaymentProvider.SystemName &&
                    x.AuthorizationTransactionResult == payload.Id);

                if (order == null)
                {
                    Logger.Error("Missing order");
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }


                var newPaymentStatus = order.PaymentStatus;
                var newOrderStatus = order.OrderStatus;

                switch (payload.Status)
                {
                    case "pending":
                        newPaymentStatus = PaymentStatus.Authorized;
                        newOrderStatus = OrderStatus.Pending;
                        break;
                    case "cancelled":
                        newOrderStatus = OrderStatus.Cancelled;
                        newPaymentStatus = PaymentStatus.Voided;
                        break;
                    case "completed":
                    case "settled":
                        newPaymentStatus = PaymentStatus.Paid;
                        newOrderStatus = OrderStatus.Processing;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var updated = false;
                if (newPaymentStatus != order.PaymentStatus)
                {
                    order.PaymentStatus = newPaymentStatus;
                    updated = true;
                }

                if (newOrderStatus != order.OrderStatus && order.OrderStatus != OrderStatus.Complete)
                {
                    order.OrderStatus = newOrderStatus;
                    updated = true;
                }

                if (updated)
                {
                    order.AddOrderNote(
                        $"SideShift {payload.Id}: Order status updated to {newOrderStatus} and payment status to {newPaymentStatus}", true);
                    order.HasNewPaymentNotification = true;
                }

                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }

    }
}
