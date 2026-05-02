# FacePass: Module 1 Technical & Functionality Report
## Desktop Kiosk Ecosystem

### 1. Overview
The Desktop Kiosk serves as the primary biometric entry point for FacePass. It integrates real-time computer vision (Emgu CV) with a high-performance WPF interface and a cloud-synchronized backend (Supabase).

### 2. Technical Architecture
The application follows the **MVVM (Model-View-ViewModel)** architectural pattern, ensuring separation between UI logic and hardware/network services.

#### 2.1 Core Technologies
- **Framework**: .NET 8.0 (WPF)
- **Computer Vision**: Emgu CV 4.10 (OpenCV Wrapper)
- **Database/Backend**: Supabase REST API (PostgreSQL)
- **QR Generation**: QRCoder 1.5.0
- **Serialization**: Newtonsoft.Json & System.Text.Json

### 3. Service Breakdown

| Service | Responsibility | Key Logic/Algorithm |
| :--- | :--- | :--- |
| `CameraService` | Hardware abstraction for Video Capture | `VideoCapture` (Emgu.CV) |
| `FaceDetectionService` | Real-time face localization | `CascadeClassifier` (Haar Cascade) |
| `FaceEncodingService` | ROI Extraction & Biometric encoding | LBPH-based grayscale serialization |
| `LivenessChallengeService` | Anti-spoofing challenge validation | EAR (Eyes), Nose Offset, Pixel Ratio (Smile) |
| `QrSessionService` | Dynamic QR & Session management | GUID-based payload + Supabase Insert |
| `SupabaseFaceRepository` | Data Persistence & Retrieval | RESTful CRUD for `students` & `logs` |

### 4. Functionality Details

#### 4.1 Biometric Workflow
1.  **Detection**: The kiosk scans the video feed at 30 FPS.
2.  **Matching**: Upon face detection, the system extracts the encoding and performs a Euclidean distance comparison against all stored `students.face_encoding` records (Threshold: `< 0.6`).
3.  **Liveness Verification**: If a match is found, the user is presented with a random challenge:
    *   **Look Left/Right**: Verified by nose tip X-coordinate deviation.
    *   **Smile**: Verified by teeth/pixel intensity ratio in the lower face ROI.
    *   **Blink**: Verified by the transient loss of eye cascade detection.
4.  **Logging**: Success triggers a `present` status; failure or timeout triggers a `suspicious` status in `attendance_logs`.

#### 4.2 Dynamic QR System
A new QR code is generated every 30 seconds. It contains a signed JSON payload with a `session_guid` (UUID), `classroom_id`, and `expires_at` timestamp. This QR is designed to be scanned by the Module 2 Mobile App for redundant verification.

### 5. UI/UX Design
The interface is designed for high visibility in academic environments:
- **Dark Mode Aesthetics**: Deep charcoal background with lime-green biometric indicators.
- **Dynamic Overlays**: Real-time bounding boxes and translucent status banners.
- **Feedback Loop**: A 3-second success banner provides immediate confirmation to the student.

### 6. Security Considerations
- **No Local Storage**: Raw images are never saved locally; all biometric data is processed in-memory.
- **VARBINARY Storage**: Encodings are stored as serialized byte arrays (BYTEA) in Supabase.
- **Anti-Spoofing**: Active liveness challenges prevent the use of static photos or videos to trick the system.

---
**Status**: Production Ready
**Last Updated**: 2026-04-29
**Author**: Lead AI Architect (Antigravity)
