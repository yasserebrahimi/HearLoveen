import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Avatar,
  Button,
  Chip,
  LinearProgress,
} from '@mui/material'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'

interface Patient {
  id: string
  name: string
  age: number
  diagnosis: string
  progressScore: number
  lastSession: string
  avatar?: string
}

export default function Patients() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [patients, setPatients] = useState<Patient[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchPatients()
  }, [])

  const fetchPatients = async () => {
    try {
      // Mock data - replace with real API call
      setPatients([
        {
          id: '1',
          name: 'Emma Johnson',
          age: 7,
          diagnosis: 'Moderate hearing loss',
          progressScore: 78,
          lastSession: '2025-01-05',
        },
        {
          id: '2',
          name: 'Liam Smith',
          age: 8,
          diagnosis: 'Severe hearing loss',
          progressScore: 65,
          lastSession: '2025-01-04',
        },
        {
          id: '3',
          name: 'Olivia Brown',
          age: 6,
          diagnosis: 'Mild hearing loss',
          progressScore: 85,
          lastSession: '2025-01-06',
        },
      ])
      setLoading(false)
    } catch (error) {
      console.error('Failed to fetch patients:', error)
      setLoading(false)
    }
  }

  const getProgressColor = (score: number) => {
    if (score >= 80) return 'success'
    if (score >= 60) return 'warning'
    return 'error'
  }

  if (loading) {
    return (
      <Box sx={{ width: '100%', mt: 4 }}>
        <LinearProgress />
      </Box>
    )
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('patient.patients') || 'Patients'}
      </Typography>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        {patients.map((patient) => (
          <Grid item xs={12} sm={6} md={4} key={patient.id}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                  <Avatar
                    sx={{ width: 56, height: 56, mr: 2, bgcolor: 'primary.main' }}
                  >
                    {patient.name.charAt(0)}
                  </Avatar>
                  <Box>
                    <Typography variant="h6">{patient.name}</Typography>
                    <Typography color="textSecondary" variant="body2">
                      {patient.age} {t('patient.yearsOld') || 'years old'}
                    </Typography>
                  </Box>
                </Box>

                <Chip
                  label={patient.diagnosis}
                  size="small"
                  sx={{ mb: 2 }}
                />

                <Typography variant="body2" color="textSecondary" gutterBottom>
                  {t('patient.progress') || 'Progress'}
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                  <Box sx={{ width: '100%', mr: 1 }}>
                    <LinearProgress
                      variant="determinate"
                      value={patient.progressScore}
                      color={getProgressColor(patient.progressScore)}
                    />
                  </Box>
                  <Typography variant="body2" color="textSecondary">
                    {patient.progressScore}%
                  </Typography>
                </Box>

                <Typography variant="caption" color="textSecondary" display="block" gutterBottom>
                  {t('patient.lastSession') || 'Last session'}: {patient.lastSession}
                </Typography>

                <Button
                  variant="outlined"
                  fullWidth
                  sx={{ mt: 2 }}
                  onClick={() => navigate(`/patient/${patient.id}`)}
                >
                  {t('patient.viewDetails') || 'View Details'}
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  )
}
