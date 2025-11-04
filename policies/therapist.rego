package hearloveen.auth
default allow = false
allow { input.user.role == "Admin" }
allow {
  input.user.role == "Therapist"
  some cid
  cid := input.resource.child_id
  cid in input.user.child_scope
}
