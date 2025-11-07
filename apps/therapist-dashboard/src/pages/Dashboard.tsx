import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Box,
  Grid,
  Paper,
  Typography,
  Card,
  CardContent,
} from '@mui/material'
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'
import axios from 'axios'

interface DashboardStats {
  totalPatients: number
  activeSessions: number
  averageProgress: number
}

export default function Dashboard() {
  const { t } = useTranslation()
  const [stats, setStats] = useState<DashboardStats>({
    totalPatients: 0,
    activeSessions: 0,
    averageProgress: 0,
  })

  const [progressData, setProgressData] = useState<any[]>([])

  useEffect(() => {
    // Fetch dashboard data
    fetchDashboardData()
  }, [])

  const fetchDashboardData = async () => {
    try {
      // Mock data - replace with real API calls
      setStats({
        totalPatients: 25,
        activeSessions: 8,
        averageProgress: 78,
      })

      setProgressData([
        { month: 'Jan', progress: 65 },
        { month: 'Feb', progress: 68 },
        { month: 'Mar', progress: 72 },
        { month: 'Apr', progress: 75 },
        { month: 'May', progress: 78 },
      ])
    } catch (error) {
      console.error('Failed to fetch dashboard data:', error)
    }
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('dashboard.welcome')}
      </Typography>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} sm={4}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                {t('dashboard.totalPatients')}
              </Typography>
              <Typography variant="h3">{stats.totalPatients}</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={4}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                {t('dashboard.activeSessions')}
              </Typography>
              <Typography variant="h3">{stats.activeSessions}</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={4}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                {t('dashboard.averageProgress')}
              </Typography>
              <Typography variant="h3">{stats.averageProgress}%</Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              {t('dashboard.recentSessions')}
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={progressData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="month" />
                <YAxis />
                <Tooltip />
                <Line type="monotone" dataKey="progress" stroke="#1976d2" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}
