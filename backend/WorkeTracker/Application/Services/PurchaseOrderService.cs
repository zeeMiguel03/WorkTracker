using Application.DTOs.Common;
using Application.DTOs.PurchaseOrder;
using Application.Interfaces;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;
namespace Application.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISourceRepository _sourceRepository;
        private readonly IEntryRepository _entryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PurchaseOrderService(
            IPurchaseOrderRepository purchaseOrderRepository, 
            ICurrentUserService currentUserService,
            ISourceRepository sourceRepository,
            IEntryRepository entryRepository,
            IUnitOfWork unitOfWork)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _currentUserService = currentUserService;
            _sourceRepository = sourceRepository;
            _entryRepository = entryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ChangePurchaseOrderStatusAsync(int purchaseOrderId, PurchaseOrderStatus status, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, currentUserId, false, cancellationToken);

            if (purchaseOrder is null)
            {
                throw new DomainException("PURCHASE_NOT_FOUND", "Purchase order was not found.");
            }

            purchaseOrder.ChangeStatus(status);

            _purchaseOrderRepository.Update(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<GetPurchaseOrderDTO> CreatePurchaseOrderAsync(CreatePurchaseOrderDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            if (dto.SourceId is int sourceId)
            {
                await ValidateSource(sourceId, currentUserId, cancellationToken);
            }

            if (dto.EntryId is int entryId)
            {
                await ValidateEntry(entryId, currentUserId, cancellationToken);
            }

            var purchaseOrder = PurchaseOrder.Create(
                currentUserId,
                dto.EntryId,
                dto.SourceId,
                dto.TrackingNumber,
                dto.ShippingCost,
                dto.OtherCosts ?? 0m,
                dto.Notes);

            await _purchaseOrderRepository.AddAsync(purchaseOrder, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToGetPurchaseOrderDTO(purchaseOrder);
        }

        public async Task DeletePurchaseOrderAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, currentUserId, false, cancellationToken);

            if (purchaseOrder is null)
            {
                throw new DomainException("PURCHASE_NOT_FOUND", "Purchase order was not found.");
            }

            _purchaseOrderRepository.Remove(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<GetPurchaseOrderDTO> GetPurchaseOrderByIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, currentUserId, false, cancellationToken);

            if (purchaseOrder is null)
            {
                throw new DomainException("PURCHASE_NOT_FOUND", "Purchase order was not found.");
            }

            return MapToGetPurchaseOrderDTO(purchaseOrder);
        }

        public async Task<PagedResultDTO<ListPurchaseOrderDTO>> ListPurchaseOrdersByUserAsync(
            int page, 
            int pageSize,
            PurchaseOrderStatus? status, 
            string? search,
            CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var purchasesOrders = await _purchaseOrderRepository.GetPageByUserIdAsync(currentUserId, page, pageSize, status, search, cancellationToken);

            return new PagedResultDTO<ListPurchaseOrderDTO>
            {
                Items = purchasesOrders.Items.Select(po => new ListPurchaseOrderDTO
                {
                    Id = po.Id,
                    SourceId = po.SourceId,
                    SourceName = po.Source?.Name,
                    TrackingNumber = po.TrackingNumber,
                    Status = po.Status,
                    ShippingCost = po.ShippingCost,
                    OtherCosts = po.OtherCosts,
                    OrderedAt = po.OrderedAt,
                    DeliveredAt = po.DeliveredAt,
                    CreatedAt = po.CreatedAt
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = purchasesOrders.TotalCount
            };
        }

        public async Task MarkPurchaseOrderAsDeliveredAsync(int purchaseOrderId, DateTime deliveredAt, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, currentUserId, false, cancellationToken);

            if (purchaseOrder is null)
            {
                throw new DomainException("PURCHASE_NOT_FOUND", "Purchase order was not found.");
            }

            purchaseOrder.MarkAsDelivered(deliveredAt);

            _purchaseOrderRepository.Update(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkPurchaseOrderAsOrderedAsync(int purchaseOrderId, DateTime orderedAt, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(purchaseOrderId, currentUserId, false, cancellationToken);

            if (purchaseOrder is null)
            {
                throw new DomainException("PURCHASE_NOT_FOUND", "Purchase order was not found.");
            }

            purchaseOrder.MarkAsOrdered(orderedAt);

            _purchaseOrderRepository.Update(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdatePurchaseOrderAsync(UpdatePurchaseOrderDTO dto, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.GetUserId();

            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(dto.Id, currentUserId, false, cancellationToken);

            if (purchaseOrder is null)
            {
                throw new DomainException("PURCHASE_NOT_FOUND", "Purchase order was not found.");
            }

            if (dto.SourceId is int sourceId)
            {
                await ValidateSource(sourceId, currentUserId, cancellationToken);
            }

            purchaseOrder.Update(
                dto.SourceId,
                dto.TrackingNumber,
                dto.ShippingCost,
                dto.OtherCosts ?? 0m,
                dto.Notes);

            _purchaseOrderRepository.Update(purchaseOrder);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static GetPurchaseOrderDTO MapToGetPurchaseOrderDTO(PurchaseOrder purchaseOrder)
        {
            return new GetPurchaseOrderDTO
            {
                Id = purchaseOrder.Id,
                EntryId = purchaseOrder.EntryId,
                SourceId = purchaseOrder.SourceId,
                SourceName = purchaseOrder.Source?.Name,
                TrackingNumber = purchaseOrder.TrackingNumber,
                Status = purchaseOrder.Status,
                ShippingCost = purchaseOrder.ShippingCost,
                OtherCosts = purchaseOrder.OtherCosts,
                OrderedAt = purchaseOrder.OrderedAt,
                DeliveredAt = purchaseOrder.DeliveredAt,
                Notes = purchaseOrder.Notes,
                CreatedAt = purchaseOrder.CreatedAt,
                UpdatedAt = purchaseOrder.UpdatedAt,
                Products = []
            };
        }

        private async Task ValidateSource(int sourceId, int currentUserId, CancellationToken cancellationToken)
        {
            var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);

            if (source is null || source.UserId != currentUserId)
            {
                throw new DomainException("SOURCE_NOT_FOUND", "Source was not found.");
            }
        }

        private async Task ValidateEntry(int entryId, int currentUserId, CancellationToken cancellationToken)
        {
            var entry = await _entryRepository.GetByIdAsync(entryId, cancellationToken);

            if (entry is null || entry.UserId != currentUserId)
            {
                throw new DomainException("ENTRY_NOT_FOUND", "Entry was not found.");
            }
        }
    }
}
