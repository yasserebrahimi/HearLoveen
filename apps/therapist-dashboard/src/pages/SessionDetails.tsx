import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Chip,
  Divider,
  Paper,
  Alert,
} from '@mui/material'
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar } from 'recharts'
import axios from 'axios'

interface SessionData {
  id: string
  date: string
  duration: number
  score: number
  transcription: string
  emotion: {
    primary: string
    confidence: number
    valence: number
    arousal: number
  }
  pronunciation: {
    score: number
    phonemes: Array<{ phoneme: string; accuracy: number }>
  }
  xaiExplanation: {
    confidence: number
    factors: string[]
    recommendations: string[]
  }
}

export default function SessionDetails() {
  const { id } = useParams<{ id: string }>()
  const { t } = useTranslation()
  const [session, setSession] = useState<SessionData | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchSessionData()
  }, [id])

  const fetchSessionData = async () => {
    try {
      // Mock data - replace with real API call to /api/analysis/session
      setSession({
        id: id || '1',
        date: '2025-01-06',
        duration: 15,
        score: 82,
        transcription: 'The quick brown fox jumps over the lazy dog.',
        emotion: {
          primary: 'happy',
          confidence: 0.87,
          valence: 0.75,
          arousal: 0.65,
        },
        pronunciation: {
          score: 82,
          phonemes: [
            { phoneme: 'th', accuracy: 0.95 },
            { phoneme: 'k', accuracy: 0.88 },
            { phoneme: 'br', accuracy: 0.75 },
            { phoneme: 'f', accuracy: 0.92 },
          ],
        },
        xaiExplanation: {
          confidence: 0.89,
          factors: [
            'Clear articulation of consonants',
            'Good rhythm and pacing',
            'Positive emotional engagement',
          ],
          recommendations: [
            'Practice "br" blend sounds',
            'Continue current speech exercises',
            'Increase session complexity gradually',
          ],
        },
      })
      setLoading(false)
    } catch (error) {
      console.error('Failed to fetch session data:', error)
      setLoading(false)
    }
  }

  if (loading || !session) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Loading...</Typography>
      </Box>
    )
  }

  const emotionData = [
    { name: 'Valence', value: session.emotion.valence * 100 },
    { name: 'Arousal', value: session.emotion.arousal * 100 },
  ]

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('session.title') || 'Session Analysis'}
      </Typography>

      <Grid container spacing={3} sx={{ mt: 2 }}>
        {/* Session Overview */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {t('session.overview') || 'Session Overview'}
              </Typography>
              <Box sx={{ mt: 2 }}>
                <Typography color="textSecondary" variant="body2">
                  {t('session.date') || 'Date'}: {session.date}
                </Typography>
                <Typography color="textSecondary" variant="body2">
                  {t('session.duration') || 'Duration'}: {session.duration} minutes
                </Typography>
                <Typography color="textSecondary" variant="body2">
                  {t('session.score') || 'Score'}: {session.score}%
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Emotion Analysis */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {t('session.emotionAnalysis') || 'Emotion Analysis'}
              </Typography>
              <Box sx={{ mt: 2 }}>
                <Chip
                  label={session.emotion.primary.toUpperCase()}
                  color="primary"
                  sx={{ mb: 2 }}
                />
                <Typography color="textSecondary" variant="body2">
                  {t('xai.confidence') || 'Confidence'}: {(session.emotion.confidence * 100).toFixed(0)}%
                </Typography>
                <ResponsiveContainer width="100%" height={150}>
                  <BarChart data={emotionData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <Tooltip />
                    <Bar dataKey="value" fill="#1976d2" />
                  </BarChart>
                </ResponsiveContainer>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Transcription */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {t('session.transcription') || 'Transcription'}
              </Typography>
              <Paper sx={{ p: 2, bgcolor: 'grey.100' }}>
                <Typography>{session.transcription}</Typography>
              </Paper>
            </CardContent>
          </Card>
        </Grid>

        {/* Pronunciation Feedback */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {t('session.pronunciationFeedback') || 'Pronunciation Feedback'}
              </Typography>
              <Typography color="textSecondary" variant="body2" gutterBottom>
                {t('session.overallScore') || 'Overall Score'}: {session.pronunciation.score}%
              </Typography>
              <Grid container spacing={2} sx={{ mt: 1 }}>
                {session.pronunciation.phonemes.map((phoneme, index) => (
                  <Grid item xs={6} sm={3} key={index}>
                    <Box>
                      <Typography variant="body2" fontWeight="bold">
                        /{phoneme.phoneme}/
                      </Typography>
                      <Typography
                        variant="caption"
                        color={phoneme.accuracy >= 0.8 ? 'success.main' : 'warning.main'}
                      >
                        {(phoneme.accuracy * 100).toFixed(0)}%
                      </Typography>
                    </Box>
                  </Grid>
                ))}
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* XAI Explanation */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {t('session.xaiExplanation') || 'AI Explanation'}
              </Typography>
              <Alert severity="info" sx={{ mb: 2 }}>
                {t('xai.confidence') || 'Confidence'}: {(session.xaiExplanation.confidence * 100).toFixed(0)}%
              </Alert>

              <Typography variant="subtitle2" gutterBottom>
                {t('xai.factors') || 'Contributing Factors'}:
              </Typography>
              <ul>
                {session.xaiExplanation.factors.map((factor, index) => (
                  <li key={index}>
                    <Typography variant="body2">{factor}</Typography>
                  </li>
                ))}
              </ul>

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" gutterBottom>
                {t('xai.recommendations') || 'Recommendations'}:
              </Typography>
              <ul>
                {session.xaiExplanation.recommendations.map((rec, index) => (
                  <li key={index}>
                    <Typography variant="body2">{rec}</Typography>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  )
}
