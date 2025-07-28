# Supabase Storage Setup Guide

## Why Supabase Storage Instead of S3?

✅ **Already integrated** in your app  
✅ **Built-in CDN** for fast global delivery  
✅ **Automatic image optimization**  
✅ **Row Level Security (RLS)** for fine-grained access control  
✅ **Cost-effective** pricing  
✅ **No additional configuration** needed

## Setup Steps

### 1. Create Storage Bucket in Supabase Dashboard

1. Go to your Supabase project dashboard: https://supabase.com/dashboard
2. Navigate to **Storage** in the left sidebar
3. Click **Create a new bucket**
4. Bucket name: `travel-images`
5. Set as **Public bucket** (for public image access)
6. Click **Create bucket**

### 2. Configure Bucket Policies (Optional but Recommended)

Go to the **Policies** tab in your bucket and add these policies:

**Policy for Public Read Access:**

```sql
CREATE POLICY "Public read access"
ON storage.objects FOR SELECT
TO public
USING ( bucket_id = 'travel-images' );
```

**Policy for Authenticated Upload:**

```sql
CREATE POLICY "Authenticated users can upload"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK ( bucket_id = 'travel-images' );
```

**Policy for Users to Delete Their Own Files:**

```sql
CREATE POLICY "Users can delete own files"
ON storage.objects FOR DELETE
TO authenticated
USING ( bucket_id = 'travel-images' AND auth.uid()::text = (storage.foldername(name))[1] );
```

### 3. Folder Structure

Your images will be organized in these folders:

```
travel-images/
├── profiles/           # User profile images
├── packages/           # Travel package images
│   ├── package-1/
│   ├── package-2/
│   └── ...
├── general/           # General uploads
└── documents/         # User documents (visa, passport, etc.)
```

### 4. Image Upload API Endpoints

#### Upload Single Image

```http
POST /api/image/upload
Authorization: Bearer {jwt_token}
Content-Type: multipart/form-data

Form Data:
- file: [image file]
- folder: "general" (optional)
- fileName: "custom_name" (optional)
```

#### Upload Multiple Images

```http
POST /api/image/upload-multiple
Authorization: Bearer {jwt_token}
Content-Type: multipart/form-data

Form Data:
- files: [multiple image files]
- folder: "packages/123"
```

#### Upload Profile Image

```http
POST /api/image/upload-profile
Authorization: Bearer {jwt_token}
Content-Type: multipart/form-data

Form Data:
- file: [profile image]
```

#### Upload Package Images

```http
POST /api/image/upload-package?packageId=123
Authorization: Bearer {jwt_token}
Content-Type: multipart/form-data

Form Data:
- files: [package images]
```

#### Get Image URL (Public)

```http
GET /api/image/url?filePath=profiles/profile_user123_20250129.jpg
```

#### Delete Image

```http
DELETE /api/image/delete?filePath=general/20250129_143022_abc123.jpg
Authorization: Bearer {jwt_token}
```

### 5. File Validation Rules

- **Max file size**: 5MB
- **Allowed formats**: .jpg, .jpeg, .png, .gif, .webp, .bmp
- **Automatic validation**: File type and MIME type checking
- **Unique filenames**: Automatic generation with timestamp and GUID

### 6. Usage in Frontend

#### JavaScript/TypeScript Example:

```javascript
// Upload profile image
const uploadProfileImage = async (file) => {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch("/api/image/upload-profile", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${authToken}`,
    },
    body: formData,
  });

  const result = await response.json();

  if (result.Success) {
    console.log("Image uploaded:", result.Data.Url);
    // Update user profile with image URL
  }
};

// Upload package images
const uploadPackageImages = async (files, packageId) => {
  const formData = new FormData();
  files.forEach((file) => formData.append("files", file));

  const response = await fetch(
    `/api/image/upload-package?packageId=${packageId}`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${authToken}`,
      },
      body: formData,
    }
  );

  const result = await response.json();

  if (result.Success) {
    console.log("Package images uploaded:", result.Data.Urls);
  }
};
```

### 7. Integration with Your Models

#### User Profile

```csharp
public class User
{
    // ... existing properties
    public string? ProfileImageUrl { get; set; }
}
```

#### Travel Package

```csharp
public class TravelPackage
{
    // ... existing properties
    public string? ImageUrl { get; set; }           // Main image
    public string? ImageGallery { get; set; }       // JSON array of image URLs
}
```

### 8. Response Format

**Success Response:**

```json
{
  "Success": true,
  "Message": "Image uploaded successfully",
  "Data": {
    "Url": "https://gkwzymyjlxmlmabjzlid.supabase.co/storage/v1/object/public/travel-images/profiles/profile_user123_20250129.jpg",
    "FilePath": "profiles/profile_user123_20250129.jpg",
    "FileSize": 1048576,
    "ContentType": "image/jpeg"
  }
}
```

**Error Response:**

```json
{
  "Success": false,
  "Error": "File size exceeds maximum allowed size of 5MB"
}
```

### 9. Security Features

- **JWT Authentication** required for uploads
- **File type validation** prevents malicious uploads
- **Size limits** prevent abuse
- **Unique filenames** prevent conflicts
- **Public read access** for displaying images
- **Private upload/delete** for security

### 10. Cost Comparison

**Supabase Storage vs AWS S3:**

- Supabase: $0.021 per GB/month (included in your existing plan)
- AWS S3: $0.023 per GB/month + data transfer costs
- **Plus**: No additional configuration, built-in CDN, seamless integration

## Quick Start

1. Create the `travel-images` bucket in Supabase
2. Your image upload API is ready to use!
3. Test with the provided HTTP file: `test-image-upload.http`
4. Integrate upload components in your frontend

That's it! No S3 configuration needed. Your image upload system is production-ready with Supabase Storage.
