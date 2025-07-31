using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.DomainModels;

// ✈️ FLIGHT BOOKING DETAILS
public class FlightBookingDetails
{
    [Required]
    public string TripType { get; set; } = string.Empty; // one-way, round-trip, multi-city
    
    [Required]
    public string DepartureCity { get; set; } = string.Empty;
    
    [Required]
    public string ArrivalCity { get; set; } = string.Empty;
    
    [Required]
    public DateTime DepartureDate { get; set; }
    
    public DateTime? ReturnDate { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int AdultPassengers { get; set; } = 1;
    
    [Range(0, 10)]
    public int ChildPassengers { get; set; } = 0;
    
    [Range(0, 10)]
    public int InfantPassengers { get; set; } = 0;
    
    [Required]
    public string PreferredClass { get; set; } = "economy"; // economy, business, first
    
    public string? PreferredAirline { get; set; }
    
    public List<PassengerInfo> Passengers { get; set; } = new();
}

public class PassengerInfo
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public string DateOfBirth { get; set; } = string.Empty;
    
    public string? PassportNumber { get; set; }
    
    public string? Nationality { get; set; }
}

// 🏨 HOTEL BOOKING DETAILS
public class HotelBookingDetails
{
    [Required]
    public string Destination { get; set; } = string.Empty;
    
    [Required]
    public DateTime CheckInDate { get; set; }
    
    [Required]
    public DateTime CheckOutDate { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int Rooms { get; set; } = 1;
    
    [Required]
    [Range(1, 20)]
    public int AdultGuests { get; set; } = 1;
    
    [Range(0, 10)]
    public int ChildGuests { get; set; } = 0;
    
    [Required]
    public string RoomType { get; set; } = "standard"; // standard, deluxe, suite
    
    public string? PreferredHotel { get; set; }
    
    public string StarRating { get; set; } = "3-star"; // 3-star, 4-star, 5-star
    
    public List<string> Amenities { get; set; } = new(); // pool, gym, spa, etc.
}

// 🗺️ TOUR BOOKING DETAILS
public class TourBookingDetails
{
    [Required]
    public string TourPackage { get; set; } = string.Empty;
    
    [Required]
    public string Destination { get; set; } = string.Empty;
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    [Range(1, 50)]
    public int Travelers { get; set; } = 1;
    
    [Required]
    public string AccommodationType { get; set; } = "standard";
    
    [Required]
    public string TourType { get; set; } = "group"; // group, private, custom
    
    public List<string> Activities { get; set; } = new();
    
    [Required]
    public string MealPlan { get; set; } = "breakfast"; // breakfast, half-board, full-board
}

// 📄 VISA BOOKING DETAILS
public class VisaBookingDetails
{
    [Required]
    public string VisaType { get; set; } = string.Empty;
    
    [Required]
    public string DestinationCountry { get; set; } = string.Empty;
    
    [Required]
    public string ProcessingType { get; set; } = "standard"; // standard, express, super-express
    
    [Required]
    public DateTime IntendedTravelDate { get; set; }
    
    [Required]
    [Range(1, 365)]
    public int DurationOfStay { get; set; }
    
    [Required]
    public string PurposeOfVisit { get; set; } = string.Empty;
    
    [Required]
    public string PassportNumber { get; set; } = string.Empty;
    
    [Required]
    public DateTime PassportExpiryDate { get; set; }
    
    [Required]
    public string Nationality { get; set; } = string.Empty;
    
    public bool HasPreviousVisa { get; set; } = false;
    
    public List<VisaDocument> RequiredDocuments { get; set; } = new();
}

public class VisaDocument
{
    [Required]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    public string DocumentName { get; set; } = string.Empty;
    
    public bool IsRequired { get; set; } = true;
    
    public bool IsUploaded { get; set; } = false;
    
    public string? FileUrl { get; set; }
}

// 📦 COMPLETE PACKAGE DETAILS
public class CompletePackageDetails
{
    public FlightBookingDetails? FlightDetails { get; set; }
    
    public HotelBookingDetails? HotelDetails { get; set; }
    
    public VisaBookingDetails? VisaDetails { get; set; }
    
    [Required]
    public string PackageType { get; set; } = string.Empty; // honeymoon, business, family, etc.
    
    [Range(0, 1000000)]
    public decimal EstimatedBudget { get; set; }
    
    public List<string> AdditionalServices { get; set; } = new(); // insurance, transfers, etc.
} 