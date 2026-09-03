import { AiChat } from './components/AiChat'
import { Navbar } from './components/Navbar'
import { Footer } from './components/Footer'
import { Hero } from './sections/Hero'
import { CapabilityStrip } from './sections/CapabilityStrip'
import { Services } from './sections/Services'
import { Booking } from './sections/Booking'
import { Skills } from './sections/Skills'
import { Projects } from './sections/Projects'
import { FeaturedProject } from './sections/FeaturedProject'
import { About } from './sections/About'
import { Philosophy } from './sections/Philosophy'
import { Process } from './sections/Process'
import { ClientTypes } from './sections/ClientTypes'
import { Contact } from './sections/Contact'

function App() {
  return (
    <>
      <Navbar />
      <main>
        <Hero />
        <CapabilityStrip />
        <Services />
        <Booking />
        <Skills />
        <Projects />
        <FeaturedProject />
        <About />
        <Philosophy />
        <Process />
        <ClientTypes />
        <Contact />
      </main>
      <Footer />
      <AiChat />
    </>
  )
}

export default App
