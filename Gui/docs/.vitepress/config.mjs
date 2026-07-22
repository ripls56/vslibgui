import { defineConfig } from 'vitepress'
import vslibLight from './theme/vslib-light.json'
import vslibDark from './theme/vslib-dark.json'

export default defineConfig({
  title: "vslibgui",
  description: "Flutter-inspired UI Framework for Vintage Story",
  srcDir: '.',
  outDir: '../.vitepress/dist',
  cacheDir: '../.vitepress/cache',
  themeConfig: {
    logo: '/images/logo.png',
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Guides', link: '/getting-started' },
      { text: 'API Reference', link: 'https://ripls56.github.io/vslibgui/api/' }
    ],
    sidebar: [
      {
        text: 'Introduction',
        items: [
          { text: 'Overview', link: '/' },
          { text: 'Getting Started', link: '/getting-started' }
        ]
      },
      {
        text: 'Core Architecture',
        items: [
          { text: 'Three-Tree Architecture', link: '/architecture' },
          { text: 'State Management', link: '/state-management' },
          { text: 'Event Handling', link: '/event-handling' },
          { text: 'Rendering Pipeline', link: '/rendering' },
          { text: 'Windowing System', link: '/windowing' }
        ]
      },
      {
        text: 'Layout & UX',
        items: [
          { text: 'Layout System', link: '/layout' },
          { text: 'Scrolling', link: '/scrolling' },
          { text: 'Animations', link: '/animations' },
          { text: 'Sound System', link: '/sound' }
        ]
      },
      {
        text: 'Widgets',
        collapsed: false,
        items: [
          { text: 'Base Classes', link: '/widgets/base' },
          { text: 'Layout Widgets', link: '/widgets/layout' },
          { text: 'Visual Widgets', link: '/widgets/visual' },
          { text: 'Interactive Widgets', link: '/widgets/interactive' },
          { text: 'Scrolling Widgets', link: '/widgets/scrolling' },
          { text: 'Complex Widgets', link: '/widgets/complex' },
          { text: 'Window Widgets', link: '/widgets/window' },
          { text: 'Animation Widgets', link: '/widgets/animation' },
          { text: 'Key Types & Misc', link: '/widgets/misc' },
          { text: 'Custom Widgets', link: '/custom-widgets' },
          { text: 'Dialogs', link: '/dialogs' }
        ]
      },
      {
        text: 'Theming',
        items: [
          { text: 'Theming Guide', link: '/theming' },
        ]
      },
      {
        text: 'Advanced',
        items: [
          { text: 'Extensibility', link: '/extensibility' },
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/ripls56/vslibgui' }
    ],
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2026 ripls56'
    },
    outline: 'deep',
    search: {
      provider: 'local'
    }
  },
  markdown: {
    theme: {
      light: vslibLight,
      dark: vslibDark
    }
  }
})
