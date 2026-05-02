import { createRouter, createWebHistory } from 'vue-router'
import Home from '../views/Home.vue'
import SessionView from '../views/SessionView.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/session/:sessionId',
    name: 'Session',
    component: SessionView
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
