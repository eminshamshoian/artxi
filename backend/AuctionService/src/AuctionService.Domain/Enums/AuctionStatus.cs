namespace AuctionService.Domain.Enums;

public enum AuctionStatus
{
    Draft = 0,          // created but not yet scheduled
    Scheduled = 1,      // starts in the future
    Live = 2,           // accepting bids
    Ended = 3,          // closed (pending settlement)
    ReserveNotMet = 4,  // ended without meeting reserve
    Cancelled = 5,      // cancelled by seller/admin
    Settled = 6         // payment/transfer complete
}