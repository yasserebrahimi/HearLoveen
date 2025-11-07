import { useState } from 'react'
import { Routes, Route } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  AppBar,
  Box,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  IconButton,
  Menu,
  MenuItem,
} from '@mui/material'
import {
  Dashboard as DashboardIcon,
  People as PeopleIcon,
  EventNote as EventNoteIcon,
  Analytics as AnalyticsIcon,
  Settings as SettingsIcon,
  Language as LanguageIcon,
} from '@mui/icons-material'
import Dashboard from './pages/Dashboard'
import Patients from './pages/Patients'
import SessionDetails from './pages/SessionDetails'

const drawerWidth = 240

export default function App() {
  const { t, i18n } = useTranslation()
  const [langAnchor, setLangAnchor] = useState<null | HTMLElement>(null)

  const changeLanguage = (lng: string) => {
    i18n.changeLanguage(lng)
    localStorage.setItem('language', lng)
    setLangAnchor(null)
  }

  const menuItems = [
    { text: t('nav.dashboard'), icon: <DashboardIcon />, path: '/' },
    { text: t('nav.patients'), icon: <PeopleIcon />, path: '/patients' },
    { text: t('nav.sessions'), icon: <EventNoteIcon />, path: '/sessions' },
    { text: t('nav.analytics'), icon: <AnalyticsIcon />, path: '/analytics' },
    { text: t('nav.settings'), icon: <SettingsIcon />, path: '/settings' },
  ]

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
        <Toolbar>
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            {t('appTitle')}
          </Typography>
          <IconButton
            color="inherit"
            onClick={(e) => setLangAnchor(e.currentTarget)}
          >
            <LanguageIcon />
          </IconButton>
          <Menu
            anchorEl={langAnchor}
            open={Boolean(langAnchor)}
            onClose={() => setLangAnchor(null)}
          >
            <MenuItem onClick={() => changeLanguage('en')}>English</MenuItem>
            <MenuItem onClick={() => changeLanguage('de')}>Deutsch</MenuItem>
            <MenuItem onClick={() => changeLanguage('nl')}>Nederlands</MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' },
        }}
      >
        <Toolbar />
        <Box sx={{ overflow: 'auto' }}>
          <List>
            {menuItems.map((item) => (
              <ListItem key={item.text} disablePadding>
                <ListItemButton component="a" href={item.path}>
                  <ListItemIcon>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.text} />
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        </Box>
      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        <Toolbar />
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/patients" element={<Patients />} />
          <Route path="/session/:id" element={<SessionDetails />} />
        </Routes>
      </Box>
    </Box>
  )
}
