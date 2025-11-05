using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.SideShift.Models;
using Smartstore.SideShift.Providers;
using Smartstore.SideShift.Settings;
using Smartstore.Web.Controllers;

namespace Smartstore.SideShift.Controllers
{
    [Route("SideShift/Hook")]
    public class SideShiftHookController(SideShiftSettings settings, SmartDbContext db) : PublicController
    {
        private readonly SmartDbContext _db = db;
        private readonly SideShiftSettings _settings = settings;

        [HttpPost]
        [WebhookEndpoint]
        public async Task<IActionResult> Post([FromBody] SideShiftWebhook payload)
        {
            try
            {                
                var order = await _db.Orders.FirstOrDefaultAsync(x =>
                    x.PaymentMethodSystemName == PaymentProvider.SystemName &&
                    x.AuthorizationTransactionResult == payload.Id);

                if (order == null)
                {
                    Logger.Error("Missing order", payload);
                    return StatusCode(StatusCodes.Status422UnprocessableEntity);
                }

                if (order.PaymentStatus == PaymentStatus.Refunded || order.OrderStatus == OrderStatus.Cancelled)
                {
                    Logger.Info("Ignoring webhook for already refunded or cancelled order", payload, order);
                    return Ok();
                }

                if (order.PaymentStatus == PaymentStatus.Paid && !string.IsNullOrEmpty(order.AuthorizationTransactionCode))
                {
                    await DoRefundWebHook(payload, order);
                }
                else
                {
                    await DoPaymentWebHook(payload, order);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }

        private async Task DoPaymentWebHook(SideShiftWebhook payload, Order order)
        {
            try
            {
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
                    var sNote = T("Plugins.Smartstore.SideShift.WebHookNote").ToString().Replace("#payloadId", payload.Id)
                                                                                        .Replace("#payloadStatus", payload.Status)
                                                                                        .Replace("#payloadSettleAmount", payload.SettleAmount)
                                                                                        .Replace("#payloadSettleCoin", payload.SettleCoin);
                    order.AddOrderNote(sNote, true);
                    order.HasNewPaymentNotification = true;
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, payload, order);
                throw;
            }
        }

        private async Task DoRefundWebHook(SideShiftWebhook payload, Order order)
        {
            try
            {
                var sNote = string.Empty;

                switch (payload.Status)
                {
                    case "cancelled":
                        sNote = T("Plugins.SmartStore.SideShift.RefundCancelled");
                        break;
                    case "completed":
                    case "settled":
                        sNote = T("Plugins.SmartStore.SideShift.RefundExecuted");
                        order.PaymentStatus = (PaymentStatus)Int32.Parse(order.AuthorizationTransactionId);
                        break;
                    default:
                        return;
                }

                order.AddOrderNote(sNote, true);
                order.HasNewPaymentNotification = true;

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, payload, order);
                throw;
            }
        }
    }
}
