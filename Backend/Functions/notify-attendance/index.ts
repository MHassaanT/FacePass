// Supabase Edge Function: notify-attendance
// Deploy with: supabase functions deploy notify-attendance

import { serve } from "https://deno.land/std@0.168.0/http/server.ts"

const FCM_SERVER_KEY = Deno.env.get('FCM_SERVER_KEY')

serve(async (req) => {
  const payload = await req.json()
  
  // payload.record contains the new attendance_log row
  const { student_id, status, course_id } = payload.record

  if (status !== 'present') {
    return new Response(JSON.ToString({ message: 'No notification for suspicious logs' }), { status: 200 })
  }

  // 1. Fetch Student FCM Token from a 'user_devices' table (to be created)
  // const { data: device } = await supabase.from('user_devices').select('fcm_token').eq('user_id', student_id).single()

  const fcmToken = "PLACEHOLDER_TOKEN" // This would be fetched from DB

  const message = {
    to: fcmToken,
    notification: {
      title: "Attendance Confirmed! ✅",
      body: `Your attendance has been marked as present.`,
      sound: "default"
    },
    data: {
      course_id: course_id,
      type: "attendance_confirmation"
    }
  }

  const response = await fetch('https://fcm.googleapis.com/fcm/send', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `key=${FCM_SERVER_KEY}`
    },
    body: JSON.stringify(message)
  })

  return new Response(await response.text(), {
    headers: { 'Content-Type': 'application/json' },
    status: 200,
  })
})
