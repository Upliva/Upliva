# UplivaAI Real Estate Demo Data

Use this data to demonstrate the complete MVP without changing the database schema.

## 1. Register Business

- Business Name: GreenView Realty & Developers
- Business Type: Real Estate
- Owner Name: Amit Kumar
- Email: amit@greenviewrealty.example
- Phone: 9876543210
- WhatsApp: 919876543210
- Address: Main Road, Ranchi
- City: Ranchi
- State: Jharkhand
- Postal Code: 834001
- Country: India
- Business Hours: Mon-Sat 9:30 AM - 7:00 PM
- Tagline: Verified homes, plots and investment properties in Ranchi
- Description: GreenView Realty helps customers discover residential plots, apartments and independent homes with local assistance and property visits.

Approve and publish the business as Admin.

## 2. Website Content

### Hero
Title: Find Your Next Property in Ranchi
Subtitle: Residential plots, apartments and independent homes with local assistance.

### About
Title: About GreenView Realty
Content: Local real-estate advisory for buyers and investors looking for residential properties in Ranchi.

### Why choose us
- Local property knowledge
- Site visit coordination
- Clear property information
- Assistance from enquiry to visit

### Services
- Residential plots
- Apartments
- Independent houses
- Property site visits
- Buyer assistance

### CTA
Title: Schedule a Property Visit
Text: Share your preferred location and budget and our team will contact you.

Enable Catalog, Offers, Testimonials and WhatsApp as required.

## 3. Sample Properties

### Property 1
- Name: GreenView Residency - 3 BHK Apartment
- Category: Apartment
- SKU: GVR-3BHK-101
- Price: ₹72 Lakh
- Location can be included in Description: Bariatu, Ranchi
- Short description: 3 BHK apartment with parking and lift.
- Description: 3 BHK, 1,650 sq ft, east-facing, covered parking, lift, gated community.
- Availability: Ready to move
- SortOrder: 1
- WhatsApp Featured: Admin selects this

### Property 2
- Name: GreenView Residency - 2 BHK Apartment
- Category: Apartment
- SKU: GVR-2BHK-102
- Price: ₹55 Lakh
- Short description: 2 BHK apartment for families and investors.
- Availability: Ready to move
- SortOrder: 2

### Property 3
- Name: Harmu Residential Plot - 1,800 sq ft
- Category: Residential Plot
- SKU: GVR-PLT-201
- Price: ₹48 Lakh
- Short description: Residential plot in a developed locality.
- Availability: Available
- SortOrder: 3

### Property 4
- Name: Morabadi Independent House - 4 BHK
- Category: Independent House
- SKU: GVR-HSE-301
- Price: ₹1.35 Crore
- Short description: Spacious 4 BHK independent house with parking.
- Availability: Available
- SortOrder: 4

### Property 5
- Name: Kokar Commercial Space - 1,200 sq ft
- Category: Commercial Property
- SKU: GVR-COM-401
- Price: ₹95 Lakh
- Short description: Road-facing commercial property suitable for office or showroom.
- Availability: Available
- SortOrder: 5

### Property 6
- Name: Dhurwa 2 BHK Investment Apartment
- Category: Apartment
- SKU: GVR-2BHK-501
- Price: ₹42 Lakh
- Short description: Investment apartment near major connectivity routes.
- Availability: Under construction
- SortOrder: 6

## 4. Admin WhatsApp Selection

For the demo, select properties 1, 3, 4 and 6 as WhatsApp Featured.

Set Featured Product Limit to 4 or 6.

WhatsApp should show only those selected properties and then direct customers to the business website for the complete catalogue.

## 5. Sample Offer

- Title: Free Site Visit This Weekend
- Discount: Free site visit
- Description: Schedule a property visit with our team this weekend.

## 6. Sample Testimonials

- Priya Sharma: The team arranged the site visit quickly and shared the property details clearly.
- Rajesh Verma: Helpful local support during our apartment search.

## 7. Brochure Demo

Upload the included sample PDF:
`SampleData/GreenView_RealEstate_Brochure.pdf`

The brochure section is automatically displayed on the UplivaAI-hosted business website when at least one PDF exists. The owner/admin can delete it at any time.

## 8. Website/domain rule

If the business has no own website/domain, use the UplivaAI `/business/{slug}` page.

If Admin configures a verified custom domain, the business domain is the canonical public URL and the fallback UplivaAI URL redirects to it.
