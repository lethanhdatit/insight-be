# Affiliate Product API Documentation

## Overview

Hệ thống API quản lý sản phẩm affiliate với hỗ trợ đa ngôn ngữ, fil**Example Request:**
```http
GET /api/affiliate/products?attributes=Màu%20sắc:Đỏ&attributes=Kích%20thước:16cm&hasDiscount=true&pageSize=10&pageNumber=1&sortBy=price_asc
```ering, matching attributes và tracking.

## Authentication

Tất cả API endpoints yêu cầu header authentication:

```
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
```

## Language Support

API hỗ trợ đa ngôn ngữ thông qua header:

```
Accept-Language: vi (Vietnamese) | en (English)
```

Default: `vi`

## Base URL

```
Production: https://api.insight.com
Development: http://localhost:60633
```

---

## 1. Get Filter Options

Lấy danh sách các options để filter sản phẩm.

**Endpoint:** `GET /api/affiliate/filter-options`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Accept-Language: vi
```

**Response:**

```json
{
  "message": "OK",
  "data": {
    "attributes": [
      {
        "name": "Màu sắc",
        "value": ["Đỏ", "Xanh", "Tím"],
        "type": "color"
      },
      {
        "name": "Chất liệu",
        "value": ["Thạch anh thiên nhiên"],
        "type": "material"
      }
    ],
    "labels": ["hot", "bestseller", "mới"],
    "categories": [
      {
        "id": "guid1",
        "code": "phong-thuy",
        "name": "Phong Thủy",
        "description": "Sản phẩm phong thủy mang lại may mắn",
        "parentId": null,
        "children": [
          {
            "id": "guid2",
            "code": "phong-thuy1",
            "name": "Phong Thủy1",
            "description": "Sản phẩm phong thủy 1",
            "parentId": "guid1",
            "children": [
                ...
            ]
          }
        ]
      }
    ]
  },
  "ts": "2025-08-07T04:30:13Z"
}
```

---

## 2. Get Products List

Lấy danh sách sản phẩm với filtering và pagination.

**Endpoint:** `GET /api/affiliate/products`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Accept-Language: vi
```

**Query Parameters:**

- `attributes[]`: Filter theo attributes, format "name:value" (e.g., `Màu sắc:Đỏ`)
- `labels[]`: Filter theo labels
- `keywords`: Tìm kiếm text
- `categoryIds[]`: Filter theo category IDs
- `hasDiscount`: true | false - Filter sản phẩm có/không có giảm giá
- `priceFrom`: Giá từ (number)
- `priceTo`: Giá đến (number)
- `provider`: shopee | lazada | tiki
- `sortBy`: relevance | price_asc | price_desc | rating_asc | rating_desc
- `pageSize`: Số items per page (1-100, default: 20)
- `pageNumber`: Trang số (default: 1)

**Example Request:**

```http
GET /api/affiliate/products?attributes=Màu%20sắc:Đỏ&attributes=Kích%20thước:16-18cm&pageSize=10&pageNumber=1&sortBy=price_asc
```

**Response:**

```json
{
  "message": "OK",
  "data": {
    "pageNumber": 1,
    "pageSize": 20,
    "totalRecords": 1,
    "totalPages": 1,
    "items": [
      {
        "id": "d05cc0a6-66be-4ccb-b44c-33b8cbb95862",
        "autoId": 6,
        "provider": "shopee",
        "providerUrl": "https://shopee.vn/product/123456",
        "status": "active",
        "price": 299000,
        "discountPrice": 199000,
        "discountPercentage": 33.44,
        "stock": 50,
        "rating": 4.5,
        "totalSold": 234,
        "name": "Vòng tay thạch anh đỏ may mắn",
        "thumbnailImage": "https://example.com/bracelet-thumb.jpg",
        "attributes": [
          {
            "name": "Màu sắc",
            "value": ["Đỏ", "Xanh", "Tím"],
            "type": "color",
            "isMatched": true
          },
          {
            "name": "Kích thước",
            "value": ["16cm", "17cm", "18cm"],
            "type": "size",
            "isMatched": true
          },
          {
            "name": "Chất liệu",
            "value": ["Thạch anh thiên nhiên"],
            "type": "material",
            "isMatched": false
          }
        ],
        "labels": ["hot", "bestseller", "mới"],
        "isFavorite": false
      }
    ]
  },
  "ts": "2025-08-07T04:30:27Z"
}
```

**Important Notes:**
- `isMatched`: true khi attribute khớp với filter, sẽ được sort lên đầu
- `attributes[]` query param có thể truyền nhiều lần
- `hasDiscount=true`: Chỉ trả về sản phẩm có giảm giá (discountPrice < price)
- `hasDiscount=false`: Chỉ trả về sản phẩm không có giảm giá
- Response trả về theo ngôn ngữ được chọn

---

## 3. Get Product Detail

Lấy chi tiết sản phẩm.

**Endpoint:** `GET /api/affiliate/products/{productId}`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Accept-Language: vi
```

**Query Parameters (Optional):**

- `attributes[]`: Filter attributes để highlight matching (same format as products list)

**Example Request:**

```http
GET /api/affiliate/products/d05cc0a6-66be-4ccb-b44c-33b8cbb95862?attributes=Màu%20sắc:Đỏ
```

**Response:**

```json
{
  "message": "OK",
  "data": {
    "id": "d05cc0a6-66be-4ccb-b44c-33b8cbb95862",
    "autoId": 6,
    "provider": "shopee",
    "providerUrl": "https://shopee.vn/product/123456",
    "status": "active",
    "name": "Vòng tay thạch anh đỏ may mắn",
    "description": "Vòng tay thạch anh đỏ thiên nhiên, mang lại may mắn và tài lộc",
    "price": 299000,
    "discountPrice": 199000,
    "discountPercentage": 33.44,
    "stock": 50,
    "rating": 4.5,
    "totalSold": 234,
    "saleLocation": "TP.HCM",
    "promotion": "Giảm 33% - Chỉ hôm nay!",
    "warranty": "Bảo hành 12 tháng",
    "shipping": "Miễn phí vận chuyển toàn quốc",
    "images": {
      "thumbnail": "https://example.com/bracelet-thumb.jpg",
      "images": [
        "https://example.com/bracelet-1.jpg",
        "https://example.com/bracelet-2.jpg"
      ]
    },
    "attributes": [
      {
        "name": "Màu sắc",
        "value": ["Đỏ", "Xanh", "Tím"],
        "type": "color",
        "isMatched": true
      }
    ],
    "labels": ["hot", "bestseller", "mới"],
    "variants": [
      {
        "name": "Kích thước",
        "imageUrl": "https://example.com/size-guide.jpg",
        "values": [
          {
            "valueText": "16cm",
            "imageUrl": "https://example.com/16cm.jpg"
          },
          {
            "valueText": "17cm",
            "imageUrl": "https://example.com/17cm.jpg"
          }
        ]
      }
    ],
    "seller": {
      "name": "PhongThuyStore",
      "imageUrl": "https://example.com/seller-logo.jpg",
      "description": "Chuyên bán đồ phong thủy chính hãng",
      "labels": [
        {
          "name": "Uy tín",
          "imageUrl": "https://example.com/trusted.jpg"
        }
      ]
    },
    "shippingOptions": {
      "options": [
        {
          "id": "standard",
          "name": "Giao hàng tiêu chuẩn",
          "description": "Giao trong 3-5 ngày làm việc",
          "price": 30000,
          "isFree": false,
          "estimatedDays": 4,
          "provider": "GHN",
          "isDefault": true
        }
      ],
      "defaultShippingId": "standard",
      "freeShippingAvailable": true,
      "freeShippingThreshold": 500000,
      "shippingFrom": "Hồ Chí Minh"
    },
    "categories": [
      {
        "id": "guid",
        "code": "phong-thuy",
        "name": "Phong Thủy",
        "description": "Sản phẩm phong thủy mang lại may mắn"
      }
    ],
    "isFavorite": false
  },
  "ts": "2025-08-07T04:30:30Z"
}
```

---

## 4. Add to Favorites

Thêm sản phẩm vào danh sách yêu thích.

**Endpoint:** `POST /api/affiliate/favorites`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

**Request Body:**

```json
{
  "productId": "d05cc0a6-66be-4ccb-b44c-33b8cbb95862"
}
```

**Response:**

```json
{
  "message": "Added to favorites successfully",
  "data": true,
  "ts": "2025-08-07T04:30:35Z"
}
```

---

## 5. Remove from Favorites

Xóa sản phẩm khỏi danh sách yêu thích.

**Endpoint:** `DELETE /api/affiliate/favorites/{productId}`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Authorization: Bearer {jwt_token}
```

**Response:**

```json
{
  "message": "Removed from favorites successfully",
  "data": true,
  "ts": "2025-08-07T04:30:40Z"
}
```

---

## 6. Get My Favorites

Lấy danh sách sản phẩm yêu thích của user.

**Endpoint:** `GET /api/affiliate/favorites`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Authorization: Bearer {jwt_token}
Accept-Language: vi
```

**Query Parameters:**

- `pageSize`: Số items per page (default: 20)
- `pageNumber`: Trang số (default: 1)

**Response:**

```json
{
  "message": "OK",
  "data": {
    "pageNumber": 1,
    "pageSize": 20,
    "totalRecords": 5,
    "totalPages": 1,
    "items": [
      {
        "id": "favorite-guid",
        "product": {
          // Same structure as products list item
        },
        "favoritedAt": "2025-08-07T04:25:00Z"
      }
    ]
  },
  "ts": "2025-08-07T04:30:45Z"
}
```

---

## 7. Track Event

Ghi lại hành vi người dùng cho analytics.

**Endpoint:** `POST /api/affiliate/tracking`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Content-Type: application/json
```

**Request Body:**

```json
{
  "productId": "d05cc0a6-66be-4ccb-b44c-33b8cbb95862",
  "categoryId": "category-guid",
  "action": "view",
  "sessionId": "session-123",
  "metaData": {
    "source": "search",
    "campaign": "summer-sale"
  }
}
```

**Action Types:**

- `view`: Xem sản phẩm
- `click`: Click vào sản phẩm
- `addToFavorite`: Thêm vào yêu thích
- `removeFromFavorite`: Xóa khỏi yêu thích
- `share`: Chia sẻ sản phẩm

**Response:**

```json
{
  "message": "OK",
  "data": true,
  "ts": "2025-08-07T04:30:50Z"
}
```

---

## 8. Seed Sample Data (Development Only)

Tạo dữ liệu mẫu cho testing.

**Endpoint:** `POST /api/affiliate/seed-data`

**Headers:**

```http
X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d
Content-Type: application/json
```

**Response:**

```json
{
  "message": "Sample data seeded successfully with shipping options!",
  "data": true,
  "ts": "2025-08-07T04:30:55Z"
}
```

---

## Error Handling

### Standard Error Response

```json
{
  "message": "Error description",
  "data": null,
  "ts": "2025-08-07T04:30:00Z"
}
```

### Common HTTP Status Codes

- `200`: Success
- `400`: Bad Request - Invalid parameters
- `401`: Unauthorized - Missing or invalid authentication
- `403`: Forbidden - Invalid API key
- `404`: Not Found - Resource not found
- `500`: Internal Server Error

### Error Examples

**Missing API Key (403):**

```json
{
  "message": "Forbidden",
  "data": null,
  "ts": "2025-08-07T04:30:00Z"
}
```

**Product Not Found (404):**

```json
{
  "message": "Product not found",
  "data": null,
  "ts": "2025-08-07T04:30:00Z"
}
```

**Unauthorized for Favorites (401):**

```json
{
  "message": "User must be logged in to add favorites",
  "data": null,
  "ts": "2025-08-07T04:30:00Z"
}
```

---

## Frontend Integration Tips

### 1. Language Switching

```javascript
// Set language in API calls
const headers = {
  "X-api-key": "fccedb68-2b84-44a8-9e12-23194e39506d",
  "Accept-Language": "vi", // or 'en'
};
```

### 2. Attribute Filtering

```javascript
// Build query string for multiple attributes
// Note: value is now an array, so you need to filter by specific values
const filters = ["Màu sắc:Đỏ", "Kích thước:16cm"];
const queryString = filters
  .map((f) => `attributes=${encodeURIComponent(f)}`)
  .join("&");
// Result: attributes=M%C3%A0u%20s%E1%BA%AFc%3A%C4%90%E1%BB%8F&attributes=K%C3%ADch%20th%C6%B0%E1%BB%9Bc%3A16cm
```

### 3. Handling IsMatched

```javascript
// Sort attributes with matched items first
product.attributes.sort((a, b) => {
  if (a.isMatched && !b.isMatched) return -1;
  if (!a.isMatched && b.isMatched) return 1;
  return a.name.localeCompare(b.name);
});

// Display attribute values (now an array)
const displayValue = attribute.value.join(", ");
```

### 4. Price Display

```javascript
// Show discount price if available, otherwise regular price
const displayPrice = product.discountPrice || product.price;
const hasDiscount = !!product.discountPrice;
```

### 5. Image Handling

```javascript
// Use thumbnail for lists, full images for detail
const thumbnailUrl = product.thumbnailImage;
const galleryImages = product.images?.images || [];
```

### 6. Pagination

```javascript
// Calculate pagination info
const totalPages = Math.ceil(totalRecords / pageSize);
const hasNextPage = pageNumber < totalPages;
const hasPrevPage = pageNumber > 1;
```

---

## Data Models

### ProductAttribute

```typescript
interface ProductAttribute {
  name: string;
  value: string[];
  type: "color" | "size" | "material" | "origin" | string;
  isMatched: boolean;
}
```

### ProductVariant

```typescript
interface ProductVariant {
  name: string;
  imageUrl: string;
  values: ProductVariantValue[];
}

interface ProductVariantValue {
  valueText: string;
  imageUrl: string;
}
```

### ProductSeller

```typescript
interface ProductSeller {
  name: string;
  imageUrl: string;
  description: string;
  labels: SellerLabel[];
}

interface SellerLabel {
  name: string;
  imageUrl: string;
}
```

### ShippingOptions

```typescript
interface ShippingOptions {
  options: ShippingOption[];
  defaultShippingId: string;
  freeShippingAvailable: boolean;
  freeShippingThreshold: number;
  shippingFrom: string;
}

interface ShippingOption {
  id: string;
  name: string;
  description: string;
  price: number;
  isFree: boolean;
  estimatedDays: number;
  provider: string;
  isDefault: boolean;
}
```

---

## Development Environment

### Setup

1. Ensure API is running on `http://localhost:60633`
2. Use seed data endpoint to create sample products
3. Test with Vietnamese (`vi`) and English (`en`) languages

### Testing Commands

```bash
# Get filter options
curl -H "X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d" \
     -H "Accept-Language: vi" \
     http://localhost:60633/api/affiliate/filter-options

# Get products with filters
curl -H "X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d" \
     -H "Accept-Language: vi" \
     "http://localhost:60633/api/affiliate/products?attributes=Màu%20sắc:Đỏ"

# Seed sample data
curl -X POST \
     -H "X-api-key: fccedb68-2b84-44a8-9e12-23194e39506d" \
     -H "Content-Type: application/json" \
     http://localhost:60633/api/affiliate/seed-data
```
